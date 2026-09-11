using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Api.Controllers;
using UserManagement.Models;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Services.Messaging;

namespace UserManagement.Api.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task GetUsers_WhenFilterIsAll_MustReturnAllUsersFromService()
    {
        // Arrange
        var controller = CreateController();
        var users = SetupUsers();

        // Act
        var result = await controller.GetUsers();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(users, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetUsers_WhenFilterIsActive_MustReturnOnlyActiveUsers()
    {
        // Arrange
        var controller = CreateController();
        var users = SetupActiveUsers();

        // Act
        var result = await controller.GetUsers(UserListFilter.Active);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(users, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetUsers_WhenFilterIsNonActive_MustReturnOnlyNonActiveUsers()
    {
        // Arrange
        var controller = CreateController();
        var users = SetupNonActiveUsers();

        // Act
        var result = await controller.GetUsers(UserListFilter.NonActive);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(users, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetUsers_MustMapEachUserToUserDto()
    {
        // Arrange
        var controller = CreateController();
        var users = SetupUsers();

        // Act
        var result = await controller.GetUsers();

        // Assert
        var dtos = result.Result.Should().BeOfType<OkObjectResult>().Which.Value
            .Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        dtos.Should().BeEquivalentTo(users, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetById_WhenUserExists_MustReturnOkWithUserDto()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupUser();

        // Act
        var result = await controller.GetById(user.Id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(user, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetById_WhenUserDoesNotExist_MustReturnNotFound()
    {
        // Arrange
        var controller = CreateController();
        _userService.Setup(s => s.GetByIdAsync(It.IsAny<long>(), It.IsAny<bool>())).ReturnsAsync((User?)null);

        // Act
        var result = await controller.GetById(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_MustPassRecordAsViewedThroughToService()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupUser();

        // Act
        await controller.GetById(user.Id, recordAsViewed: true);

        // Assert
        _userService.Verify(s => s.GetByIdAsync(user.Id, true), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenRecordAsViewedNotSpecified_MustDefaultToFalse()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupUser();

        // Act
        await controller.GetById(user.Id);

        // Assert
        _userService.Verify(s => s.GetByIdAsync(user.Id, false), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenUserDoesNotExist_MustLogTheMissingIdAtInformation()
    {
        // Arrange
        var controller = CreateController();
        _userService.Setup(s => s.GetByIdAsync(It.IsAny<long>(), It.IsAny<bool>())).ReturnsAsync((User?)null);

        // Act
        await controller.GetById(999);

        // Assert
        _logger.Collector.GetSnapshot().Should().ContainSingle()
            .Which.Should().Match<FakeLogRecord>(r => r.Level == LogLevel.Information && r.Message.Contains("999"));
    }

    [Fact]
    public async Task GetUserLogs_MustReturnGetForUserAsyncItemsMappedToDto()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupUser();
        var logs = new[]
        {
            new UserLog { Id = 1, UserId = user.Id, Action = UserManagement.Models.UserLogAction.Created, Timestamp = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc) }
        };
        _userLogService.Setup(s => s.GetForUserAsync(user.Id)).ReturnsAsync(logs);

        // Act
        var result = await controller.GetUserLogs(user.Id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<UserLogDto>>()
            .Which.Should().ContainSingle(l => l.Id == 1 && l.UserId == user.Id);
    }

    [Fact]
    public async Task Create_WhenValid_MustReturnAcceptedAtTheCommandStatusEndpointWithTheCommandId()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.Create(NewCreateRequest());

        // Assert
        AssertAcceptedAtCommandStatus(result);
    }

    [Fact]
    public async Task Create_WhenValid_MustPublishACreateUserCommandCarryingTheRequestFields()
    {
        // Arrange
        var controller = CreateController();
        var request = NewCreateRequest();
        var published = CapturePublishedCommands();

        // Act
        var result = await controller.Create(request);

        // Assert
        var command = published.Should().ContainSingle().Which.Should().BeOfType<CreateUserCommand>().Subject;
        command.CommandId.Should().Be(AcceptedCommandId(result));
        command.Forename.Should().Be(request.Forename);
        command.Surname.Should().Be(request.Surname);
        command.Email.Should().Be(request.Email);
        command.DateOfBirth.Should().Be(request.DateOfBirth);
        command.IsActive.Should().Be(request.IsActive);
    }

    [Fact]
    public async Task Create_WhenValid_MustPublishThePasswordHashNeverTheClearText()
    {
        // Arrange
        var controller = CreateController();
        var request = NewCreateRequest(password: "12345");
        _credentialService.Setup(c => c.SetPassword(It.IsAny<User>(), "12345")).Callback<User, string>((u, _) => u.PasswordHash = "hashed-12345");
        var published = CapturePublishedCommands();

        // Act
        await controller.Create(request);

        // Assert
        _credentialService.Verify(c => c.SetPassword(It.Is<User>(u => u.Email == request.Email), "12345"), Times.Once);
        published.Should().ContainSingle().Which.Should().BeOfType<CreateUserCommand>()
            .Which.PasswordHash.Should().Be("hashed-12345");
    }

    [Fact]
    public async Task Create_WhenValid_MustMarkTheCommandPendingBeforePublishingIt()
    {
        // Arrange
        var controller = CreateController();
        var marked = CaptureMarkedPending();

        // Act
        var result = await controller.Create(NewCreateRequest());

        // Assert
        marked.CommandId.Should().Be(AcceptedCommandId(result));
        marked.BeforePublish.Should().BeTrue();
    }

    [Fact]
    public async Task Create_WhenValid_MustNotCreateTheUserItself()
    {
        // Arrange
        var controller = CreateController();

        // Act
        await controller.Create(NewCreateRequest());

        // Assert
        _userService.Verify(s => s.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenUserExists_MustReturnAcceptedAtStatusEndpoint()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing");

        // Act
        var result = await controller.Update(5, NewUpdateRequest());

        // Assert
        AssertAcceptedAtCommandStatus(result);
    }

    [Fact]
    public async Task Update_WhenUserExists_MustPublishAnUpdateUserCommandWithTheIdAndTheRequestFields()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing");
        var request = NewUpdateRequest();
        var published = CapturePublishedCommands();

        // Act
        var result = await controller.Update(5, request);

        // Assert
        var command = published.Should().ContainSingle().Which.Should().BeOfType<UpdateUserCommand>().Subject;
        command.CommandId.Should().Be(AcceptedCommandId(result));
        command.UserId.Should().Be(5);
        command.Forename.Should().Be(request.Forename);
        command.Surname.Should().Be(request.Surname);
        command.Email.Should().Be(request.Email);
        command.DateOfBirth.Should().Be(request.DateOfBirth);
        command.IsActive.Should().Be(request.IsActive);
    }

    [Fact]
    public async Task Update_WhenPasswordSupplied_MustPublishTheNewHash()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing", passwordHash: "old-hash");
        _credentialService.Setup(c => c.SetPassword(It.IsAny<User>(), "new-secret")).Callback<User, string>((u, _) => u.PasswordHash = "hashed-new-secret");
        var published = CapturePublishedCommands();

        // Act
        await controller.Update(5, NewUpdateRequest(password: "new-secret"));

        // Assert
        _credentialService.Verify(c => c.SetPassword(It.Is<User>(u => u.Id == 5), "new-secret"), Times.Once);
        published.Should().ContainSingle().Which.Should().BeOfType<UpdateUserCommand>()
            .Which.PasswordHash.Should().Be("hashed-new-secret");
    }

    [Fact]
    public async Task Update_WhenPasswordNotSupplied_MustPublishNoHash()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing", passwordHash: "old-hash");
        var published = CapturePublishedCommands();

        // Act
        await controller.Update(5, NewUpdateRequest(password: null));

        // Assert
        _credentialService.Verify(c => c.SetPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        published.Should().ContainSingle().Which.Should().BeOfType<UpdateUserCommand>()
            .Which.PasswordHash.Should().BeNull();
    }

    [Fact]
    public async Task Update_WhenUserExists_MustMarkTheCommandPendingBeforePublishingIt()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing");
        var marked = CaptureMarkedPending();

        // Act
        var result = await controller.Update(5, NewUpdateRequest());

        // Assert
        marked.CommandId.Should().Be(AcceptedCommandId(result));
        marked.BeforePublish.Should().BeTrue();
    }

    [Fact]
    public async Task Update_WhenUserExists_MustNotUpdateTheUserItself()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing");

        // Act
        await controller.Update(5, NewUpdateRequest());

        // Assert
        _userService.Verify(s => s.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenUserDoesNotExist_MustReturnNotFoundAndPublishNothing()
    {
        // Arrange
        var controller = CreateController();
        _userService.Setup(s => s.GetByIdAsync(999, It.IsAny<bool>())).ReturnsAsync((User?)null);

        // Act
        var result = await controller.Update(999, NewUpdateRequest());

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _messageBus.Verify(b => b.PublishAsync(It.IsAny<ICommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _statusStore.Verify(s => s.MarkPendingAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Delete_MustReturnAcceptedAtTheCommandStatusEndpointWithTheCommandId()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.Delete(5);

        // Assert
        AssertAcceptedAtCommandStatus(result);
    }

    [Fact]
    public async Task Delete_MustPublishADeleteUserCommandWithTheId()
    {
        // Arrange
        var controller = CreateController();
        var published = CapturePublishedCommands();

        // Act
        var result = await controller.Delete(5);

        // Assert
        var command = published.Should().ContainSingle().Which.Should().BeOfType<DeleteUserCommand>().Subject;
        command.CommandId.Should().Be(AcceptedCommandId(result));
        command.UserId.Should().Be(5);
    }

    [Fact]
    public async Task Delete_MustMarkTheCommandPendingBeforePublishingIt()
    {
        // Arrange
        var controller = CreateController();
        var marked = CaptureMarkedPending();

        // Act
        var result = await controller.Delete(5);

        // Assert
        marked.CommandId.Should().Be(AcceptedCommandId(result));
        marked.BeforePublish.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_MustNotDeleteTheUserItself()
    {
        // Arrange
        var controller = CreateController();

        // Act
        await controller.Delete(5);

        // Assert
        _userService.Verify(s => s.DeleteAsync(It.IsAny<long>()), Times.Never);
    }

    private static void AssertAcceptedAtCommandStatus(ActionResult<CommandAcceptedResponse> result)
    {
        var accepted = result.Result.Should().BeOfType<AcceptedAtActionResult>().Subject;
        accepted.ActionName.Should().Be(nameof(CommandsController.GetStatus));
        accepted.ControllerName.Should().Be("Commands");
        var body = accepted.Value.Should().BeOfType<CommandAcceptedResponse>().Subject;
        body.CommandId.Should().NotBeEmpty();
        accepted.RouteValues.Should().NotBeNull();
        accepted.RouteValues!["id"].Should().Be(body.CommandId);
    }

    private static Guid AcceptedCommandId(ActionResult<CommandAcceptedResponse> result)
        => ((CommandAcceptedResponse)((AcceptedAtActionResult)result.Result!).Value!).CommandId;

    private List<ICommand> CapturePublishedCommands()
    {
        var published = new List<ICommand>();
        _messageBus
            .Setup(b => b.PublishAsync(It.IsAny<ICommand>(), It.IsAny<CancellationToken>()))
            .Callback<ICommand, CancellationToken>((c, _) => published.Add(c))
            .Returns(Task.CompletedTask);
        return published;
    }

    private sealed class MarkedPending
    {
        public Guid? CommandId { get; set; }
        public bool BeforePublish { get; set; }
    }

    private MarkedPending CaptureMarkedPending()
    {
        var marked = new MarkedPending();
        _statusStore
            .Setup(s => s.MarkPendingAsync(It.IsAny<Guid>()))
            .Callback<Guid>(id =>
            {
                marked.CommandId = id;
                marked.BeforePublish = !_messageBus.Invocations.Any(i => i.Method.Name == nameof(IMessageBus.PublishAsync));
            })
            .Returns(Task.CompletedTask);
        return marked;
    }

    private static CreateUserRequest NewCreateRequest(string password = "12345") => new()
    {
        Forename = "Brand New",
        Surname = "User",
        Email = "brandnewuser@example.com",
        DateOfBirth = new DateOnly(1995, 4, 12),
        IsActive = true,
        Password = password
    };

    private static UpdateUserRequest NewUpdateRequest(string? password = null) => new()
    {
        Forename = "Updated",
        Surname = "User",
        Email = "updated@example.com",
        DateOfBirth = new DateOnly(1995, 4, 12),
        IsActive = true,
        Password = password
    };

    private User SetupUser(long id = 1, string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true, DateOnly? dateOfBirth = null, string? passwordHash = null)
    {
        var user = new User
        {
            Id = id,
            Forename = forename,
            Surname = surname,
            Email = email,
            IsActive = isActive,
            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1),
            PasswordHash = passwordHash
        };

        _userService.Setup(s => s.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);

        return user;
    }

    private User[] SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Id = 1,
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = true,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _userService.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

        return users;
    }

    private User[] SetupActiveUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Id = 1,
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = true,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _userService.Setup(s => s.FilterByActiveAsync(true)).ReturnsAsync(users);

        return users;
    }

    private User[] SetupNonActiveUsers(string forename = "Jane", string surname = "User", string email = "juser2@example.com", DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Id = 2,
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = false,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _userService.Setup(s => s.FilterByActiveAsync(false)).ReturnsAsync(users);

        return users;
    }

    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IUserLogService> _userLogService = new();
    private readonly Mock<ICredentialService> _credentialService = new();
    private readonly Mock<IMessageBus> _messageBus = new();
    private readonly Mock<ICommandStatusStore> _statusStore = new();
    private readonly FakeLogger<UsersController> _logger = new();
    private UsersController CreateController() => new(_userService.Object, _userLogService.Object, _credentialService.Object, _messageBus.Object, _statusStore.Object, _logger);
}

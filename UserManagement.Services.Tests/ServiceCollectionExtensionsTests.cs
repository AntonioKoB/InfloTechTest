using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Services.Messaging;

namespace UserManagement.Data.Tests;

/// <summary>
/// Pins what the registration alone decides: the decorator order (the user cache sits beneath the audit, so
/// a read served from memory is still recorded as a view), that every command has a handler, and that an
/// audit entry leaves as one command on the bus rather than being written inline. Resolves through the real
/// AddDomainServices with only the data access mocked.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddDomainServices_WhenAUserIsReadTwiceAsViewed_MustHitTheDatabaseOnceAndPublishTwoViewedLogCommands()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var user = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        var dataContext = new Mock<IDataContext>();
        dataContext.Setup(d => d.GetByIdAsync<User>(It.IsAny<object>())).ReturnsAsync(user);
        using var provider = new ServiceCollection().AddSingleton(dataContext.Object).AddDomainServices().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // Act: Invokes the method under test with the arranged parameters.
        await userService.GetByIdAsync(5, recordAsViewed: true);
        await userService.GetByIdAsync(5, recordAsViewed: true);

        // Assert: Verifies that the action of the method under test behaves as expected.
        dataContext.Verify(d => d.GetByIdAsync<User>(It.IsAny<object>()), Times.Once);
        var published = await provider.GetRequiredService<IMessageBus>().ReadAsync(3, TimeSpan.FromMilliseconds(300));
        published.Should().HaveCount(2).And.AllSatisfy(c =>
            c.Should().BeOfType<RecordUserLogCommand>().Which.Entry.Should().Match<UserLog>(l => l.UserId == 5 && l.Action == UserLogAction.Viewed));
        dataContext.Verify(d => d.CreateAsync(It.IsAny<UserLog>()), Times.Never);
    }

    [Fact]
    public void AddDomainServices_MustResolveAHandlerForEveryCommand()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        using var provider = new ServiceCollection().AddSingleton(new Mock<IDataContext>().Object).AddDomainServices().BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act: Invokes the method under test with the arranged parameters.
        var handlers = new object[]
        {
            scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateUserCommand>>(),
            scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateUserCommand>>(),
            scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteUserCommand>>(),
            scope.ServiceProvider.GetRequiredService<ICommandHandler<RecordUserLogCommand>>()
        };

        // Assert: Verifies that the action of the method under test behaves as expected.
        handlers.Should().AllSatisfy(h => h.Should().NotBeNull());
    }

    [Fact]
    public async Task AddDomainServices_WhenACreateCommandIsHandled_MustPublishExactlyOneLogCommandAndWriteNoLogInline()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // The handler writes through the decorated IUserService. If the auditing decorator still recorded
        // Created on its own, the same write would produce two log commands.
        var dataContext = new Mock<IDataContext>();
        dataContext.Setup(d => d.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);
        dataContext.Setup(d => d.CreateAsync(It.IsAny<User>())).Callback<User>(u => u.Id = 7).Returns(Task.CompletedTask);
        using var provider = new ServiceCollection().AddSingleton(dataContext.Object).AddDomainServices().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateUserCommand>>();
        var command = new CreateUserCommand(Guid.NewGuid(), "Brand New", "User", "brandnewuser@example.com", new DateOnly(1995, 4, 12), true, "hashed");

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        var published = await provider.GetRequiredService<IMessageBus>().ReadAsync(2, TimeSpan.FromMilliseconds(300));
        published.Should().ContainSingle().Which.Should().BeOfType<RecordUserLogCommand>()
            .Which.Entry.Should().Match<UserLog>(l => l.UserId == 7 && l.Action == UserLogAction.Created);
        dataContext.Verify(d => d.CreateAsync(It.IsAny<UserLog>()), Times.Never);
    }
}

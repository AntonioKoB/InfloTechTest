using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Controllers;
using UserManagement.Services.Commands;
using ContractCommandState = UserManagement.Api.Contracts.Commands.CommandState;

namespace UserManagement.Api.Tests;

/// <summary>
/// The status endpoint is how a caller learns what happened to a command the API accepted: the new user's id
/// on a completed create, or the reason on a failure (a duplicate email lands here, not in a 400).
/// </summary>
public class CommandsControllerTests
{
    [Fact]
    public async Task GetStatus_WhenTheIdIsUnknown_MustReturnNotFound()
    {
        // Arrange
        var controller = CreateController();
        _statusStore.Setup(s => s.GetAsync(It.IsAny<Guid>())).ReturnsAsync((CommandStatus?)null);

        // Act
        var result = await controller.GetStatus(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetStatus_WhenTheIdIsUnknown_MustLogTheIdAtInformation()
    {
        // Arrange
        var controller = CreateController();
        var id = Guid.NewGuid();
        _statusStore.Setup(s => s.GetAsync(id)).ReturnsAsync((CommandStatus?)null);

        // Act
        await controller.GetStatus(id);

        // Assert
        _logger.Collector.GetSnapshot().Should().ContainSingle()
            .Which.Should().Match<FakeLogRecord>(r => r.Level == LogLevel.Information && r.Id.Id == 1301 && r.Message.Contains(id.ToString()));
    }

    [Fact]
    public async Task GetStatus_WhenPending_MustReturnPendingWithNoUserIdOrError()
    {
        // Arrange
        var controller = CreateController();
        var id = Guid.NewGuid();
        _statusStore.Setup(s => s.GetAsync(id)).ReturnsAsync(new CommandStatus(id, UserManagement.Services.Commands.CommandState.Pending, null, null));

        // Act
        var result = await controller.GetStatus(id);

        // Assert
        Body(result).Should().BeEquivalentTo(new CommandStatusResponse { CommandId = id, State = ContractCommandState.Pending, UserId = null, Error = null });
    }

    [Fact]
    public async Task GetStatus_WhenCompleted_MustReturnCompletedWithTheAffectedUserId()
    {
        // Arrange
        var controller = CreateController();
        var id = Guid.NewGuid();
        _statusStore.Setup(s => s.GetAsync(id)).ReturnsAsync(new CommandStatus(id, UserManagement.Services.Commands.CommandState.Completed, 42, null));

        // Act
        var result = await controller.GetStatus(id);

        // Assert
        Body(result).Should().BeEquivalentTo(new CommandStatusResponse { CommandId = id, State = ContractCommandState.Completed, UserId = 42, Error = null });
    }

    [Fact]
    public async Task GetStatus_WhenFailed_MustReturnFailedWithTheError()
    {
        // Arrange
        var controller = CreateController();
        var id = Guid.NewGuid();
        _statusStore.Setup(s => s.GetAsync(id)).ReturnsAsync(new CommandStatus(id, UserManagement.Services.Commands.CommandState.Failed, null, "A user with email 'taken@example.com' already exists."));

        // Act
        var result = await controller.GetStatus(id);

        // Assert
        Body(result).Should().BeEquivalentTo(new CommandStatusResponse { CommandId = id, State = ContractCommandState.Failed, UserId = null, Error = "A user with email 'taken@example.com' already exists." });
    }

    private static CommandStatusResponse Body(ActionResult<CommandStatusResponse> result)
        => result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<CommandStatusResponse>().Subject;

    private readonly Mock<ICommandStatusStore> _statusStore = new();
    private readonly FakeLogger<CommandsController> _logger = new();
    private CommandsController CreateController() => new(_statusStore.Object, _logger);
}

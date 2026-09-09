using System;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Blazor.Api;
using UserManagement.Blazor.Components.Logs;

namespace UserManagement.Blazor.Tests;

public class LogRowTests : BunitContext
{
    public LogRowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(_logsApi.Object);
    }

    [Fact]
    public void CollapsedRow_MustShowUserActionTimestampOnly()
    {
        // Arrange
        var log = SetupLogListItem();

        // Act
        var cut = Render<LogRow>(p => p.Add(x => x.Log, log));

        // Assert
        cut.Markup.Should().Contain(log.UserId.ToString())
            .And.Contain(log.Action.ToString())
            .And.Contain(log.Timestamp.ToString());
    }

    [Fact]
    public void ClickCollapsedRow_MustExpandAndFetchDiff()
    {
        // Arrange
        var log = SetupLogListItem();
        var detail = new UserLogDto { Id = log.Id, UserId = log.UserId, Action = log.Action, Timestamp = log.Timestamp, Changes = [] };
        _logsApi.Setup(a => a.GetLogByIdAsync(log.Id)).ReturnsAsync(detail);
        var cut = Render<LogRow>(p => p.Add(x => x.Log, log));

        // Act
        cut.Find("#log-row-header").Click();

        // Assert
        _logsApi.Verify(a => a.GetLogByIdAsync(log.Id), Times.Once);
    }

    [Fact]
    public void ExpandedRow_MustShowFieldChanges()
    {
        // Arrange
        var log = SetupLogListItem(action: UserLogAction.Updated);
        var detail = new UserLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            Action = log.Action,
            Timestamp = log.Timestamp,
            Changes = [new FieldChangeDto { PropertyName = "Email", OldValue = "old@example.com", NewValue = "new@example.com" }]
        };
        _logsApi.Setup(a => a.GetLogByIdAsync(log.Id)).ReturnsAsync(detail);
        var cut = Render<LogRow>(p => p.Add(x => x.Log, log));

        // Act
        cut.Find("#log-row-header").Click();

        // Assert
        cut.Find("#log-changes").TextContent.Should().Contain("Email").And.Contain("old@example.com").And.Contain("new@example.com");
    }

    [Fact]
    public void ExpandedRow_ForViewedAction_MustShowNoChanges()
    {
        // Arrange
        var log = SetupLogListItem(action: UserLogAction.Viewed);
        var detail = new UserLogDto { Id = log.Id, UserId = log.UserId, Action = log.Action, Timestamp = log.Timestamp, Changes = [] };
        _logsApi.Setup(a => a.GetLogByIdAsync(log.Id)).ReturnsAsync(detail);
        var cut = Render<LogRow>(p => p.Add(x => x.Log, log));

        // Act
        cut.Find("#log-row-header").Click();

        // Assert
        cut.Find("#log-changes").TextContent.Should().Contain("No changes recorded");
    }

    [Fact]
    public void ExpandCollapseExpand_MustNotRefetchDiff()
    {
        // Arrange
        var log = SetupLogListItem();
        var detail = new UserLogDto { Id = log.Id, UserId = log.UserId, Action = log.Action, Timestamp = log.Timestamp, Changes = [] };
        _logsApi.Setup(a => a.GetLogByIdAsync(log.Id)).ReturnsAsync(detail);
        var cut = Render<LogRow>(p => p.Add(x => x.Log, log));

        // Act
        cut.Find("#log-row-header").Click();
        cut.Find("#log-row-header").Click();
        cut.Find("#log-row-header").Click();

        // Assert
        _logsApi.Verify(a => a.GetLogByIdAsync(log.Id), Times.Once);
    }

    private static UserLogDto SetupLogListItem(long id = 1, long userId = 5, UserLogAction action = UserLogAction.Created)
        => new()
        {
            Id = id,
            UserId = userId,
            Action = action,
            Timestamp = new DateTime(2026, 9, 1, 12, 0, 0),
            Changes = []
        };

    private readonly Mock<ILogsApi> _logsApi = new();
}

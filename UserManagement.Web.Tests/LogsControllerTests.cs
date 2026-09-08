using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Models;
using UserManagement.Services.Domain;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.Logs;
using UserManagement.WebMS.Controllers;

namespace UserManagement.Data.Tests;

public class LogsControllerTests
{
    [Fact]
    public async Task List_WhenPageNotSpecified_MustRequestPageOneWithConfiguredPageSize()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        SetupPagedResult(page: 1, pageSize: 10);

        // Act: Invokes the method under test with the arranged parameters.
        await controller.List();

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userLogService.Verify(s => s.GetPagedAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task List_WhenPageSpecified_MustPassPageToService()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        SetupPagedResult(page: 3, pageSize: 10);

        // Act: Invokes the method under test with the arranged parameters.
        await controller.List(3);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userLogService.Verify(s => s.GetPagedAsync(3, 10), Times.Once);
    }

    [Fact]
    public async Task List_WhenServiceReturnsPagedLogs_MustMapItemsAndPagingInfoOntoViewModel()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var logs = new[]
        {
            new UserLog { Id = 1, UserId = 7, Action = UserLogAction.Created, Timestamp = new DateTime(2026, 9, 8, 9, 0, 0, DateTimeKind.Utc) }
        };
        _userLogService
            .Setup(s => s.GetPagedAsync(1, 10))
            .ReturnsAsync(new PagedResult<UserLog> { Items = logs, Page = 1, PageSize = 10, TotalCount = 21 });

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.List();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Model.Should().BeOfType<LogListViewModel>()
            .Which.Should().BeEquivalentTo(new LogListViewModel
            {
                Items = [new LogListItemViewModel { Id = 1, UserId = 7, Action = UserLogAction.Created, Timestamp = logs[0].Timestamp }],
                Page = 1,
                TotalPages = 3
            });
    }

    [Fact]
    public async Task View_WhenLogExists_MustReturnViewResultWithDetail()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var log = new UserLog { Id = 5, UserId = 7, Action = UserLogAction.Updated, Timestamp = new DateTime(2026, 9, 8, 9, 0, 0, DateTimeKind.Utc) };
        var changes = new[] { new FieldChange { PropertyName = "Forename", OldValue = "Old", NewValue = "New" } };
        _userLogService.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(log);
        _diffBuilder.Setup(b => b.Build(log)).Returns(changes);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.View(5);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<LogDetailViewModel>()
            .Which.Should().BeEquivalentTo(new LogDetailViewModel
            {
                UserId = 7,
                Action = UserLogAction.Updated,
                Timestamp = log.Timestamp,
                Changes = [new LogFieldChangeViewModel { PropertyName = "Forename", OldValue = "Old", NewValue = "New" }]
            });
    }

    private void SetupPagedResult(int page, int pageSize)
        => _userLogService
            .Setup(s => s.GetPagedAsync(page, pageSize))
            .ReturnsAsync(new PagedResult<UserLog> { Items = [], Page = page, PageSize = pageSize, TotalCount = 0 });

    private readonly Mock<IUserLogService> _userLogService = new();
    private readonly Mock<IUserLogDiffBuilder> _diffBuilder = new();
    private LogsController CreateController() => new(_userLogService.Object, _diffBuilder.Object);
}

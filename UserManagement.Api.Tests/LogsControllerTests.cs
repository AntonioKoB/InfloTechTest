using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Controllers;
using UserManagement.Models;
using UserManagement.Services.Domain;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Tests;

public class LogsControllerTests
{
    [Fact]
    public async Task GetLogs_WhenPageNotSpecified_MustDefaultToPageOne()
    {
        // Arrange
        var controller = CreateController();
        SetupPage(page: 1, pageSize: 10);

        // Act
        await controller.GetLogs();

        // Assert
        _userLogService.Verify(s => s.GetPagedAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task GetLogs_MustPassPageAndPageSizeToService()
    {
        // Arrange
        var controller = CreateController();
        SetupPage(page: 2, pageSize: 5);

        // Act
        await controller.GetLogs(page: 2, pageSize: 5);

        // Assert
        _userLogService.Verify(s => s.GetPagedAsync(2, 5), Times.Once);
    }

    [Fact]
    public async Task GetLogs_MustMapItemsWithEmptyChanges()
    {
        // Arrange
        var controller = CreateController();
        SetupPage(page: 1, pageSize: 10);

        // Act
        var result = await controller.GetLogs();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PagedResultDto<UserLogDto>>()
            .Which.Items.Should().OnlyContain(i => i.Changes.Count == 0);
    }

    [Fact]
    public async Task GetLogs_MustMapPagingFieldsOntoResponse()
    {
        // Arrange
        var controller = CreateController();
        SetupPage(page: 2, pageSize: 5, totalCount: 17);

        // Act
        var result = await controller.GetLogs(page: 2, pageSize: 5);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PagedResultDto<UserLogDto>>()
            .Which.Should().BeEquivalentTo(new { Page = 2, PageSize = 5, TotalCount = 17 }, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetById_WhenLogExists_MustReturnOkWithDiffFromDiffBuilder()
    {
        // Arrange
        var controller = CreateController();
        var log = new UserLog { Id = 1, UserId = 5, Action = UserManagement.Models.UserLogAction.Updated, Timestamp = DateTime.UtcNow };
        _userLogService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(log);
        var changes = new[] { new FieldChange { PropertyName = "Email", OldValue = "old@example.com", NewValue = "new@example.com" } };
        _diffBuilder.Setup(b => b.Build(log)).Returns(changes);

        // Act
        var result = await controller.GetById(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<UserLogDto>()
            .Which.Changes.Should().ContainSingle(c => c.PropertyName == "Email" && c.OldValue == "old@example.com" && c.NewValue == "new@example.com");
    }

    [Fact]
    public async Task GetById_WhenLogDoesNotExist_MustReturnNotFound()
    {
        // Arrange
        var controller = CreateController();
        _userLogService.Setup(s => s.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((UserLog?)null);

        // Act
        var result = await controller.GetById(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_ForViewedAction_MustReturnEmptyChangesList()
    {
        // Arrange
        var controller = CreateController();
        var log = new UserLog { Id = 2, UserId = 5, Action = UserManagement.Models.UserLogAction.Viewed, Timestamp = DateTime.UtcNow };
        _userLogService.Setup(s => s.GetByIdAsync(2)).ReturnsAsync(log);
        _diffBuilder.Setup(b => b.Build(log)).Returns([]);

        // Act
        var result = await controller.GetById(2);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<UserLogDto>()
            .Which.Changes.Should().BeEmpty();
    }

    private void SetupPage(int page, int pageSize, int totalCount = 1)
    {
        var items = new[]
        {
            new UserLog { Id = 1, UserId = 1, Action = UserManagement.Models.UserLogAction.Created, Timestamp = DateTime.UtcNow }
        };

        _userLogService.Setup(s => s.GetPagedAsync(page, pageSize)).ReturnsAsync(new PagedResult<UserLog>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    private readonly Mock<IUserLogService> _userLogService = new();
    private readonly Mock<IUserLogDiffBuilder> _diffBuilder = new();
    private LogsController CreateController() => new(_userLogService.Object, _diffBuilder.Object);
}

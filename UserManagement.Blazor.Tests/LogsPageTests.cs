using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Blazor.Api;
using UserManagement.Blazor.Components.Logs;
using UserManagement.Blazor.Components.Pages;

namespace UserManagement.Blazor.Tests;

public class LogsPageTests : BunitContext
{
    public LogsPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(_logsApi.Object);
    }

    [Fact]
    public void OnInitialized_MustLoadFirstPage()
    {
        // Arrange
        SetupPage(page: 1, totalCount: 1);

        // Act
        var cut = Render<LogsPage>();

        // Assert
        _logsApi.Verify(a => a.GetLogsAsync(1, It.IsAny<int>()), Times.Once);
        cut.FindComponents<LogRow>().Should().HaveCount(1);
    }

    [Fact]
    public void ClickPageTwo_MustReload()
    {
        // Arrange
        SetupPage(page: 1, totalCount: 25);
        SetupPage(page: 2, totalCount: 25);
        var cut = Render<LogsPage>();

        // Act
        cut.Find("#next-page-button").Click();

        // Assert
        _logsApi.Verify(a => a.GetLogsAsync(2, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void MustRenderPaginationFromApiResponse()
    {
        // Arrange
        SetupPage(page: 1, totalCount: 25, pageSize: 10);

        // Act
        var cut = Render<LogsPage>();

        // Assert
        cut.Find("#page-info").TextContent.Should().Contain("1").And.Contain("3");
    }

    private void SetupPage(int page, int totalCount, int pageSize = 10)
    {
        var items = Enumerable.Range(1, Math.Min(pageSize, totalCount))
            .Select(i => new UserLogDto { Id = i, UserId = 1, Action = UserLogAction.Created, Timestamp = DateTime.UtcNow, Changes = [] })
            .ToArray();

        _logsApi.Setup(a => a.GetLogsAsync(page, It.IsAny<int>())).ReturnsAsync(new PagedResultDto<UserLogDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    private readonly Mock<ILogsApi> _logsApi = new();
}

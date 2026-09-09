using Microsoft.AspNetCore.Components;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Components.Pages;

public partial class LogsPage
{
    [Inject] private ILogsApi LogsApi { get; set; } = default!;

    private const int PageSize = 10;
    private int _page = 1;
    private int _totalPages = 1;
    private IReadOnlyList<UserLogDto> _items = [];

    protected override async Task OnInitializedAsync() => await LoadAsync(1);

    private async Task LoadAsync(int page)
    {
        var result = await LogsApi.GetLogsAsync(page, PageSize);
        _items = result.Items;
        _page = result.Page;
        _totalPages = result.TotalPages;
    }

    private Task PreviousPage() => LoadAsync(_page - 1);
    private Task NextPage() => LoadAsync(_page + 1);
}

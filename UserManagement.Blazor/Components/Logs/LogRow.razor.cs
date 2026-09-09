using Microsoft.AspNetCore.Components;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Components.Logs;

public partial class LogRow
{
    [Parameter, EditorRequired] public UserLogDto Log { get; set; } = default!;

    [Inject] private ILogsApi LogsApi { get; set; } = default!;

    private bool _expanded;
    private bool _fetched;
    private IReadOnlyList<FieldChangeDto> _changes = [];

    private async Task ToggleExpand()
    {
        if (!_fetched)
        {
            var detail = await LogsApi.GetLogByIdAsync(Log.Id);
            _changes = detail.Changes;
            _fetched = true;
        }

        _expanded = !_expanded;
    }
}

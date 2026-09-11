using System.Linq;
using Microsoft.AspNetCore.Components;
using UserManagement.Api.Contracts.Users;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Components.Pages;

public partial class UsersPage
{
    [Inject] private IUsersApi UsersApi { get; set; } = default!;

    private List<RowState> _rows = [];
    private UserListFilter _filter = UserListFilter.All;
    private bool _showAddModal;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync(UserListFilter.All);
    }

    private async Task LoadAsync(UserListFilter filter)
    {
        _filter = filter;
        var users = await UsersApi.GetUsersAsync(filter);
        _rows = users.Select(u => new RowState(Guid.NewGuid(), u)).ToList();
    }

    private void OpenAddModal() => _showAddModal = true;

    // Only accepted so far and the new id is unknown, so reload the list.
    private async Task HandleUserAdded()
    {
        _showAddModal = false;
        await LoadAsync(_filter);
    }

    private void HandleAddCancelled() => _showAddModal = false;

    private void HandleSaved(Guid rowKey, UserDto saved)
    {
        var index = _rows.FindIndex(r => r.RowKey == rowKey);
        if (index >= 0)
        {
            _rows[index] = new RowState(rowKey, saved);
        }
    }

    private void RemoveRow(Guid rowKey) => _rows.RemoveAll(r => r.RowKey == rowKey);

    private record RowState(Guid RowKey, UserDto User);
}

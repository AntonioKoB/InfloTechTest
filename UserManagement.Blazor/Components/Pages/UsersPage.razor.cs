using System.Linq;
using Microsoft.AspNetCore.Components;
using UserManagement.Api.Contracts.Users;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Components.Pages;

public partial class UsersPage
{
    [Inject] private IUsersApi UsersApi { get; set; } = default!;

    private List<RowState> _rows = [];
    private bool _showAddModal;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync(UserListFilter.All);
    }

    private async Task LoadAsync(UserListFilter filter)
    {
        var users = await UsersApi.GetUsersAsync(filter);
        _rows = users.Select(u => new RowState(Guid.NewGuid(), u)).ToList();
    }

    private void OpenAddModal() => _showAddModal = true;

    private void HandleUserAdded(UserDto created)
    {
        _rows.Add(new RowState(Guid.NewGuid(), created));
        _showAddModal = false;
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

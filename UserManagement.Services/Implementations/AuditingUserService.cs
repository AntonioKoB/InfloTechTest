using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

/// <summary>
/// Records a Viewed entry for GetByIdAsync calls that opt in through recordAsViewed. Writes pass through;
/// their entries are recorded by the command handlers.
/// </summary>
public class AuditingUserService : IUserService
{
    private readonly IUserService _inner;
    private readonly IUserLogService _userLogService;

    public AuditingUserService(IUserService inner, IUserLogService userLogService)
    {
        _inner = inner;
        _userLogService = userLogService;
    }

    public Task<IEnumerable<User>> FilterByActiveAsync(bool isActive) => _inner.FilterByActiveAsync(isActive);
    public Task<IEnumerable<User>> GetAllAsync() => _inner.GetAllAsync();
    public Task<User?> GetByEmailAsync(string email) => _inner.GetByEmailAsync(email);
    public async Task<User?> GetByIdAsync(long id, bool recordAsViewed = false)
    {
        var user = await _inner.GetByIdAsync(id, recordAsViewed);
        if (recordAsViewed && user is not null)
            await _userLogService.RecordAsync(user.Id, UserLogAction.Viewed, before: null, after: user);
        return user;
    }

    public Task CreateAsync(User user) => _inner.CreateAsync(user);
    public Task UpdateAsync(User user) => _inner.UpdateAsync(user);
    public Task DeleteAsync(long id) => _inner.DeleteAsync(id);
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

/// <summary>
/// Decorates IUserService, recording an audit log entry for every successful Create/Update/Delete, and for
/// GetByIdAsync calls that opt in via recordAsViewed. GetByIdAsync is shared by the View screen, Edit's form
/// pre-fill, Edit's own re-fetch before saving, and Delete's confirmation screen - only the caller knows
/// which of these it is, so it says so via recordAsViewed rather than the decorator guessing from context.
/// </summary>
public class AuditingUserService : IUserService
{
    private readonly IUserService _inner;
    private readonly IUserLogService _userLogService;
    private readonly IDataContext _dataAccess;

    public AuditingUserService(IUserService inner, IUserLogService userLogService, IDataContext dataAccess)
    {
        _inner = inner;
        _userLogService = userLogService;
        _dataAccess = dataAccess;
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

    public async Task CreateAsync(User user)
    {
        await _inner.CreateAsync(user);
        await _userLogService.RecordAsync(user.Id, UserLogAction.Created, before: null, after: user);
    }

    public async Task UpdateAsync(User user)
    {
        var before = await _dataAccess.FirstOrDefaultAsync<User>(u => u.Id == user.Id);
        await _inner.UpdateAsync(user);
        await _userLogService.RecordAsync(user.Id, UserLogAction.Updated, before, after: user);
    }

    public async Task DeleteAsync(long id)
    {
        var before = await _dataAccess.FirstOrDefaultAsync<User>(u => u.Id == id);
        await _inner.DeleteAsync(id);
        if (before is not null)
            await _userLogService.RecordAsync(id, UserLogAction.Deleted, before, after: null);
    }
}

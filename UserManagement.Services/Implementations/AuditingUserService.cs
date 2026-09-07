using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

/// <summary>
/// Decorates IUserService, recording an audit log entry for every successful Create/Update/Delete.
/// GetByIdAsync is deliberately not audited here - it's shared by the View screen, Edit's form pre-fill,
/// Edit's own re-fetch before saving, and Delete's confirmation screen, and the decorator can't tell which
/// of these a call is for. Only the View controller action records "Viewed" explicitly, since it's the one
/// unambiguous case.
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
    public Task<User?> GetByIdAsync(long id) => _inner.GetByIdAsync(id);

    public Task CreateAsync(User user) => throw new NotImplementedException();

    public Task UpdateAsync(User user) => throw new NotImplementedException();

    public Task DeleteAsync(long id) => throw new NotImplementedException();
}

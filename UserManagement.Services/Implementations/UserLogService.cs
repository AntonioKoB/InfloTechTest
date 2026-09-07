using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

public class UserLogService : IUserLogService
{
    private readonly IDataContext _dataAccess;
    public UserLogService(IDataContext dataAccess) => _dataAccess = dataAccess;

    public Task RecordAsync(long userId, UserLogAction action, User? before, User? after)
        => throw new NotImplementedException();

    public Task<IEnumerable<UserLog>> GetForUserAsync(long userId)
        => throw new NotImplementedException();
}

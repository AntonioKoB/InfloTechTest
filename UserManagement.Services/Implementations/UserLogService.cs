using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

public class UserLogService : IUserLogService
{
    private readonly IDataContext _dataAccess;
    public UserLogService(IDataContext dataAccess) => _dataAccess = dataAccess;

    public Task RecordAsync(long userId, UserLogAction action, User? before, User? after)
        => _dataAccess.CreateAsync(new UserLog
        {
            UserId = userId,
            Action = action,
            Timestamp = DateTime.UtcNow,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after)
        });

    public async Task<IEnumerable<UserLog>> GetForUserAsync(long userId)
        => (await _dataAccess.GetAllAsync<UserLog>())
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp);

    public async Task<PagedResult<UserLog>> GetPagedAsync(int page, int pageSize)
    {
        var logs = (await _dataAccess.GetAllAsync<UserLog>())
            .OrderByDescending(l => l.Timestamp)
            .ToList();

        return new PagedResult<UserLog>
        {
            Items = logs.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = logs.Count
        };
    }
}

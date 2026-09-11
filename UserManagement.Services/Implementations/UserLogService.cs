using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Services.Messaging;

namespace UserManagement.Services.Domain.Implementations;

public class UserLogService : IUserLogService
{
    private readonly IDataContext _dataAccess;
    private readonly IMessageBus _messageBus;

    public UserLogService(IDataContext dataAccess, IMessageBus messageBus)
    {
        _dataAccess = dataAccess;
        _messageBus = messageBus;
    }

    // Builds the finished entry here (snapshots serialized at the moment of the action) and hands it to the
    // bus; RecordUserLogCommandHandler writes it, off the caller's path.
    public Task RecordAsync(long userId, UserLogAction action, User? before, User? after)
        => _messageBus.PublishAsync(new RecordUserLogCommand(Guid.NewGuid(), new UserLog
        {
            UserId = userId,
            Action = action,
            Timestamp = DateTime.UtcNow,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after)
        }));

    public async Task<IEnumerable<UserLog>> GetForUserAsync(long userId)
        => (await _dataAccess.GetAllAsync<UserLog>())
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp);

    public async Task<PagedResult<UserLog>> GetPagedAsync(int page, int pageSize)
    {
        var items = await _dataAccess.GetPageAsync<UserLog, DateTime>(l => l.Timestamp, descending: true, skip: (page - 1) * pageSize, take: pageSize);
        var totalCount = await _dataAccess.CountAsync<UserLog>();

        return new PagedResult<UserLog>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task<UserLog?> GetByIdAsync(long id) => _dataAccess.GetByIdAsync<UserLog>(id);
}

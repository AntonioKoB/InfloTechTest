using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Models;

namespace UserManagement.Services.Domain.Interfaces;

public interface IUserLogService
{
    /// <summary>
    /// Record an action performed against a user, with optional before/after snapshots
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="action"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    Task RecordAsync(long userId, UserLogAction action, User? before, User? after);

    /// <summary>
    /// Return all log entries recorded against a given user, newest first
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<IEnumerable<UserLog>> GetForUserAsync(long userId);
}

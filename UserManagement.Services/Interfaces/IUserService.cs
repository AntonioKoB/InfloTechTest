using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Models;

namespace UserManagement.Services.Domain.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Return users by active state
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    Task<IEnumerable<User>> FilterByActiveAsync(bool isActive);
    Task<IEnumerable<User>> GetAllAsync();

    /// <summary>
    /// Return a single user matching the given ID, or null if none exists
    /// </summary>
    /// <param name="id"></param>
    /// <param name="recordAsViewed">
    /// Whether this fetch represents a genuine "user looked at this user's details" event, e.g. the View
    /// screen - as opposed to an internal lookup (Edit's form pre-fill, Edit's own re-fetch before saving,
    /// Delete's confirmation screen). When true, a "Viewed" entry is recorded in the audit log.
    /// </param>
    /// <returns></returns>
    Task<User?> GetByIdAsync(long id, bool recordAsViewed = false);

    /// <summary>
    /// Return a single user matching the given email, or null if none exists
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task CreateAsync(User user);

    /// <summary>
    /// Update an existing user
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task UpdateAsync(User user);

    /// <summary>
    /// Delete an existing user matching the given ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task DeleteAsync(long id);
}

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
    /// <param name="recordAsViewed">True when the fetch is a genuine view (the View screen), not an internal lookup.</param>
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

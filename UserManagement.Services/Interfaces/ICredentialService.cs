using System.Threading.Tasks;
using UserManagement.Models;

namespace UserManagement.Services.Domain.Interfaces;

public interface ICredentialService
{
    /// <summary>
    /// Hash the password and store only the hash on the user. Does not persist.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="password"></param>
    void SetPassword(User user, string password);

    /// <summary>
    /// Return the user for a matching email and password, or null.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    Task<User?> AuthenticateAsync(string email, string password);

    /// <summary>
    /// End the user's session server-side. A no-op today; kept so sign-out is audited and revocation has a
    /// home.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task SignOutAsync(long userId);
}

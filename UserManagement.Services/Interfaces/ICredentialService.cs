using System.Threading.Tasks;
using UserManagement.Models;

namespace UserManagement.Services.Domain.Interfaces;

public interface ICredentialService
{
    /// <summary>
    /// Hash the given plain-text password and store only the hash on the user. Does not persist - the
    /// caller saves the user through IUserService as usual. The plain-text password is never stored.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="password"></param>
    void SetPassword(User user, string password);

    /// <summary>
    /// Return the user matching the given email and password, or null if the email is unknown, the
    /// password is wrong, or the user is not active.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    Task<User?> AuthenticateAsync(string email, string password);

    /// <summary>
    /// End the given user's session on the server side. Tokens are stateless, so there is nothing to revoke
    /// today - the call exists so that signing out is audited exactly like signing in, and so that token
    /// revocation can be added here later without touching any caller.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task SignOutAsync(long userId);
}

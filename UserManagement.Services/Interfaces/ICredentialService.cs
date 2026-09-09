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
}

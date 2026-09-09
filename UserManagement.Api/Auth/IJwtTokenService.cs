using UserManagement.Models;

namespace UserManagement.Api.Auth;

public interface IJwtTokenService
{
    /// <summary>
    /// Issue a signed bearer token identifying the given (already authenticated) user, valid for the
    /// configured lifetime.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    IssuedToken CreateToken(User user);
}

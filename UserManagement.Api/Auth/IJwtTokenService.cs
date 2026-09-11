using UserManagement.Models;

namespace UserManagement.Api.Auth;

public interface IJwtTokenService
{
    /// <summary>
    /// Issue a signed bearer token for an authenticated user.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    IssuedToken CreateToken(User user);
}

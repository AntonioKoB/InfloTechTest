using Microsoft.Extensions.Options;
using UserManagement.Models;

namespace UserManagement.Api.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public IssuedToken CreateToken(User user) => throw new NotImplementedException();
}

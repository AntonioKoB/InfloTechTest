using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

public class CredentialService : ICredentialService
{
    private readonly IUserService _userService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public CredentialService(IUserService userService, IPasswordHasher<User> passwordHasher)
    {
        _userService = userService;
        _passwordHasher = passwordHasher;
    }

    public void SetPassword(User user, string password)
        => user.PasswordHash = _passwordHasher.HashPassword(user, password);

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _userService.GetByEmailAsync(email);
        if (user is null || !user.IsActive || user.PasswordHash is null)
            return null;

        // SuccessRehashNeeded still means the password was right.
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    // Nothing to revoke while tokens are stateless; the audit entry is the whole effect.
    public Task SignOutAsync(long userId) => Task.CompletedTask;
}

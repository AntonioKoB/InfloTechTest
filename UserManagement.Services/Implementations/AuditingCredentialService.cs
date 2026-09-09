using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

/// <summary>
/// Decorates ICredentialService, recording a LoggedIn entry for every successful authentication and a
/// LoggedOut entry for every sign-out - the session boundaries of a user's audit trail. Failed attempts
/// are not recorded: there is no verified user to attribute them to.
/// </summary>
public class AuditingCredentialService : ICredentialService
{
    private readonly ICredentialService _inner;
    private readonly IUserLogService _userLogService;

    public AuditingCredentialService(ICredentialService inner, IUserLogService userLogService)
    {
        _inner = inner;
        _userLogService = userLogService;
    }

    public void SetPassword(User user, string password) => _inner.SetPassword(user, password);

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _inner.AuthenticateAsync(email, password);
        if (user is not null)
            await _userLogService.RecordAsync(user.Id, UserLogAction.LoggedIn, before: null, after: null);
        return user;
    }

    public async Task SignOutAsync(long userId)
    {
        await _inner.SignOutAsync(userId);
        await _userLogService.RecordAsync(userId, UserLogAction.LoggedOut, before: null, after: null);
    }
}

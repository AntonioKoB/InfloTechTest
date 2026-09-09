using System;
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

    public void SetPassword(User user, string password) => throw new NotImplementedException();

    public Task<User?> AuthenticateAsync(string email, string password) => throw new NotImplementedException();
}

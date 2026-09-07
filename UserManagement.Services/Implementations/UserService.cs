using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

public class UserService : IUserService
{
    private readonly IDataContext _dataAccess;
    public UserService(IDataContext dataAccess) => _dataAccess = dataAccess;

    /// <summary>
    /// Return users by active state
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    public async Task<IEnumerable<User>> FilterByActiveAsync(bool isActive)
        => (await _dataAccess.GetAllAsync<User>()).Where(u => u.IsActive == isActive);

    public Task<IEnumerable<User>> GetAllAsync() => _dataAccess.GetAllAsync<User>();

    public Task<User?> GetByIdAsync(long id) => _dataAccess.GetByIdAsync<User>(id);

    public Task<User?> GetByEmailAsync(string email)
        => _dataAccess.FirstOrDefaultAsync<User>(u => u.Email.ToLower() == email.ToLower());

    public async Task CreateAsync(User user)
    {
        if (await GetByEmailAsync(user.Email) is not null)
        {
            throw new EmailAlreadyExistsException(user.Email);
        }

        await _dataAccess.CreateAsync(user);
    }

    public async Task UpdateAsync(User user)
    {
        var existingUser = await GetByEmailAsync(user.Email);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            throw new EmailAlreadyExistsException(user.Email);
        }

        await _dataAccess.UpdateAsync(user);
    }

    public Task DeleteAsync(long id) => throw new NotImplementedException();
}

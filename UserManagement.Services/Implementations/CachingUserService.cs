using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Caching;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

public class CachingUserService : IUserService
{
    public CachingUserService(IUserService inner, ICache cache)
    {
    }

    public Task<IEnumerable<User>> FilterByActiveAsync(bool isActive) => throw new NotImplementedException();
    public Task<IEnumerable<User>> GetAllAsync() => throw new NotImplementedException();
    public Task<User?> GetByIdAsync(long id, bool recordAsViewed = false) => throw new NotImplementedException();
    public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
    public Task CreateAsync(User user) => throw new NotImplementedException();
    public Task UpdateAsync(User user) => throw new NotImplementedException();
    public Task DeleteAsync(long id) => throw new NotImplementedException();
}

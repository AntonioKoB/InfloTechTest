using UserManagement.Api.Contracts.Users;
using UserManagement.Models;

namespace UserManagement.Api.Mapping;

public static class UserMappingExtensions
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Forename = user.Forename,
        Surname = user.Surname,
        Email = user.Email,
        IsActive = user.IsActive,
        DateOfBirth = user.DateOfBirth
    };

    public static User ToUser(this CreateUserRequest request) => new()
    {
        Forename = request.Forename,
        Surname = request.Surname,
        Email = request.Email,
        DateOfBirth = request.DateOfBirth!.Value,
        IsActive = request.IsActive
    };

    public static void ApplyTo(this UpdateUserRequest request, User user)
    {
        user.Forename = request.Forename;
        user.Surname = request.Surname;
        user.Email = request.Email;
        user.DateOfBirth = request.DateOfBirth!.Value;
        user.IsActive = request.IsActive;
    }
}

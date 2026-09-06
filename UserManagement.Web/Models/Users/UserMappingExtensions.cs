using UserManagement.Models;

namespace UserManagement.Web.Models.Users;

public static class UserMappingExtensions
{
    public static UserListItemViewModel ToListItemViewModel(this User user) => new()
    {
        Id = user.Id,
        Forename = user.Forename,
        Surname = user.Surname,
        Email = user.Email,
        IsActive = user.IsActive,
        DateOfBirth = user.DateOfBirth
    };

    public static UserViewModel ToViewModel(this User user) => new()
    {
        Id = user.Id,
        Forename = user.Forename,
        Surname = user.Surname,
        Email = user.Email,
        IsActive = user.IsActive,
        DateOfBirth = user.DateOfBirth
    };
}

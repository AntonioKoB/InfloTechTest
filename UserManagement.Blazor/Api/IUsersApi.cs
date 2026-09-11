using Refit;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Contracts.Users;

namespace UserManagement.Blazor.Api;

public interface IUsersApi
{
    [Get("/api/users")]
    Task<IReadOnlyList<UserDto>> GetUsersAsync([Query] UserListFilter filter = UserListFilter.All);

    [Get("/api/users/{id}")]
    Task<UserDto> GetUserByIdAsync(long id, [Query] bool recordAsViewed = false);

    [Get("/api/users/{id}/logs")]
    Task<IReadOnlyList<UserManagement.Api.Contracts.Logs.UserLogDto>> GetUserLogsAsync(long id);

    [Post("/api/users")]
    Task<CommandAcceptedResponse> CreateUserAsync([Body] CreateUserRequest request);

    [Put("/api/users/{id}")]
    Task<CommandAcceptedResponse> UpdateUserAsync(long id, [Body] UpdateUserRequest request);

    [Delete("/api/users/{id}")]
    Task DeleteUserAsync(long id);
}

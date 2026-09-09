using Refit;
using UserManagement.Api.Contracts.Logs;

namespace UserManagement.Blazor.Api;

public interface ILogsApi
{
    [Get("/api/logs")]
    Task<PagedResultDto<UserLogDto>> GetLogsAsync([Query] int page = 1, [Query] int pageSize = 10);

    [Get("/api/logs/{id}")]
    Task<UserLogDto> GetLogByIdAsync(long id);
}

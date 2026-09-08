using UserManagement.Models;
using UserManagement.Web.Models.Users;

namespace UserManagement.Web.Models.Logs;

public static class UserLogMappingExtensions
{
    public static LogListItemViewModel ToListItemViewModel(this UserLog log) => new()
    {
        UserId = log.UserId,
        Action = log.Action,
        Timestamp = log.Timestamp
    };

    public static UserLogEntryViewModel ToEntryViewModel(this UserLog log) => new()
    {
        Action = log.Action,
        Timestamp = log.Timestamp
    };
}

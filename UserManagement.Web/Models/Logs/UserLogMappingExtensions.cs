using UserManagement.Models;
using UserManagement.Services.Domain;
using UserManagement.Web.Models.Users;

namespace UserManagement.Web.Models.Logs;

public static class UserLogMappingExtensions
{
    public static LogListItemViewModel ToListItemViewModel(this UserLog log) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        Action = log.Action,
        Timestamp = log.Timestamp
    };

    public static UserLogEntryViewModel ToEntryViewModel(this UserLog log) => new()
    {
        Id = log.Id,
        Action = log.Action,
        Timestamp = log.Timestamp
    };

    public static LogFieldChangeViewModel ToViewModel(this FieldChange change) => new()
    {
        PropertyName = change.PropertyName,
        OldValue = change.OldValue,
        NewValue = change.NewValue
    };
}

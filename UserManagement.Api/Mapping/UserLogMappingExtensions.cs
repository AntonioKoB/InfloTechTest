using UserManagement.Api.Contracts.Logs;
using UserManagement.Services.Domain;

namespace UserManagement.Api.Mapping;

public static class UserLogMappingExtensions
{
    public static UserLogDto ToDto(this UserManagement.Models.UserLog log, IReadOnlyList<FieldChange>? changes = null) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        Action = ToContractAction(log.Action),
        Timestamp = log.Timestamp,
        Changes = changes is null ? [] : [.. changes.Select(c => c.ToDto())]
    };

    public static FieldChangeDto ToDto(this FieldChange change) => new()
    {
        PropertyName = change.PropertyName,
        OldValue = change.OldValue,
        NewValue = change.NewValue
    };

    private static UserLogAction ToContractAction(UserManagement.Models.UserLogAction action) => action switch
    {
        UserManagement.Models.UserLogAction.Created => UserLogAction.Created,
        UserManagement.Models.UserLogAction.Viewed => UserLogAction.Viewed,
        UserManagement.Models.UserLogAction.Updated => UserLogAction.Updated,
        UserManagement.Models.UserLogAction.Deleted => UserLogAction.Deleted,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };
}

using UserManagement.Api.Contracts.Logs;
using UserManagement.Services.Domain;
using DomainAction = UserManagement.Models.UserLogAction;
using DomainLog = UserManagement.Models.UserLog;

namespace UserManagement.Api.Mapping;

public static class UserLogMappingExtensions
{
    public static UserLogDto ToDto(this DomainLog log, IReadOnlyList<FieldChange>? changes = null) => new()
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

    private static UserLogAction ToContractAction(DomainAction action) => action switch
    {
        DomainAction.Created => UserLogAction.Created,
        DomainAction.Viewed => UserLogAction.Viewed,
        DomainAction.Updated => UserLogAction.Updated,
        DomainAction.Deleted => UserLogAction.Deleted,
        DomainAction.LoggedIn => UserLogAction.LoggedIn,
        DomainAction.LoggedOut => UserLogAction.LoggedOut,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };
}

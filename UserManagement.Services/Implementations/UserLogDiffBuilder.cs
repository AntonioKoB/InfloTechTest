using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UserManagement.Models;
using UserManagement.Services.Domain;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

public class UserLogDiffBuilder : IUserLogDiffBuilder
{
    public IReadOnlyList<FieldChange> Build(UserLog log)
    {
        // Only Created, Updated and Deleted describe a change to the user. Viewed carries an After snapshot
        // (for display on the View screen) that would otherwise look identical to Created's "nothing -> full
        // state" shape, and LoggedIn/LoggedOut carry no snapshot at all. Nothing changed in any of them, so
        // there is nothing to diff.
        if (log.Action is UserLogAction.Viewed or UserLogAction.LoggedIn or UserLogAction.LoggedOut)
            return [];

        var before = log.BeforeJson is null ? null : JsonSerializer.Deserialize<User>(log.BeforeJson);
        var after = log.AfterJson is null ? null : JsonSerializer.Deserialize<User>(log.AfterJson);

        // Id is identity, not a business field. Anything [JsonIgnore]d is excluded from the snapshots by
        // definition (the credential hash being the case in point), so there is nothing to diff - and it
        // must never be surfaced on screen regardless.
        var properties = typeof(User).GetProperties()
            .Where(p => p.Name != nameof(User.Id) && !p.IsDefined(typeof(JsonIgnoreAttribute), inherit: true));

        var changes = new List<FieldChange>();
        foreach (var property in properties)
        {
            var oldValue = before is null ? null : property.GetValue(before)?.ToString();
            var newValue = after is null ? null : property.GetValue(after)?.ToString();

            if (before is not null && after is not null && oldValue == newValue)
                continue;

            changes.Add(new FieldChange { PropertyName = property.Name, OldValue = oldValue, NewValue = newValue });
        }

        return changes;
    }
}

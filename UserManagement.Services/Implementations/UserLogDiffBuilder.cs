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
        // Only Created, Updated and Deleted describe a change. Viewed carries a snapshot for display only;
        // LoggedIn and LoggedOut carry none.
        if (log.Action is UserLogAction.Viewed or UserLogAction.LoggedIn or UserLogAction.LoggedOut)
            return [];

        var before = log.BeforeJson is null ? null : JsonSerializer.Deserialize<User>(log.BeforeJson);
        var after = log.AfterJson is null ? null : JsonSerializer.Deserialize<User>(log.AfterJson);

        // Id is identity, and anything [JsonIgnore]d is not in the snapshots.
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

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UserManagement.Models;
using UserManagement.Services.Domain;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

public class UserLogDiffBuilder : IUserLogDiffBuilder
{
    public IReadOnlyList<FieldChange> Build(UserLog log)
    {
        var before = log.BeforeJson is null ? null : JsonSerializer.Deserialize<User>(log.BeforeJson);
        var after = log.AfterJson is null ? null : JsonSerializer.Deserialize<User>(log.AfterJson);

        var properties = typeof(User).GetProperties().Where(p => p.Name != nameof(User.Id));

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

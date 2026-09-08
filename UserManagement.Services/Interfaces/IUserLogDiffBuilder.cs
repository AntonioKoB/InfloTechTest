using System.Collections.Generic;
using UserManagement.Models;
using UserManagement.Services.Domain;

namespace UserManagement.Services.Domain.Interfaces;

public interface IUserLogDiffBuilder
{
    /// <summary>
    /// Compute the field-level changes represented by a log entry's before/after snapshots
    /// </summary>
    /// <param name="log"></param>
    /// <returns></returns>
    IReadOnlyList<FieldChange> Build(UserLog log);
}

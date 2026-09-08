using System;
using System.Collections.Generic;
using UserManagement.Models;
using UserManagement.Services.Domain;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

public class UserLogDiffBuilder : IUserLogDiffBuilder
{
    public IReadOnlyList<FieldChange> Build(UserLog log)
        => throw new NotImplementedException();
}

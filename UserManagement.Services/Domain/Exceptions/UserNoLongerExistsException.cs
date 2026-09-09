using System;

namespace UserManagement.Services.Domain.Exceptions;

public class UserNoLongerExistsException : Exception
{
    public UserNoLongerExistsException(long id) : base($"User with id '{id}' no longer exists.")
    {
    }
}

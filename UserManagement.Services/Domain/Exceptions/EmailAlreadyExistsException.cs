using System;

namespace UserManagement.Services.Domain.Exceptions;

public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email) : base($"A user with email '{email}' already exists.")
    {
        Email = email;
    }

    public string Email { get; }
}

using System;

namespace UserManagement.Data.Exceptions;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

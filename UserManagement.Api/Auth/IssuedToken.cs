namespace UserManagement.Api.Auth;

public record IssuedToken(string Value, DateTime ExpiresAtUtc);

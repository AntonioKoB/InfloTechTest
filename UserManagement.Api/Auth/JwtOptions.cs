namespace UserManagement.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";

    /// <summary>
    /// HMAC-SHA256 signing key, at least 32 bytes. User-secrets or environment, never committed.
    /// </summary>
    public string SigningKey { get; set; } = "";

    public int ExpiryMinutes { get; set; } = 60;
}

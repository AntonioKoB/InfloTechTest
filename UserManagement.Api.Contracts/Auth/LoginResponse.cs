namespace UserManagement.Api.Contracts.Auth;

public class LoginResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
}

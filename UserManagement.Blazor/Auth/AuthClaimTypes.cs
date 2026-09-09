namespace UserManagement.Blazor.Auth;

public static class AuthClaimTypes
{
    /// <summary>
    /// Claim on the signed-in principal carrying the API bearer token issued at login.
    /// </summary>
    public const string AccessToken = "access_token";
}

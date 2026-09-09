namespace UserManagement.Api.Contracts.Users;

public class CreateUserRequest
{
    [Required]
    public string Forename { get; set; } = "";

    [Required]
    public string Surname { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public DateOnly? DateOfBirth { get; set; }

    public bool IsActive { get; set; } = true;
}

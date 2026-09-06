using System;
using System.ComponentModel.DataAnnotations;

namespace UserManagement.Web.Models.Users;

public class UserFormViewModel
{
    [Required]
    public string Forename { get; set; } = string.Empty;

    [Required]
    public string Surname { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateOnly? DateOfBirth { get; set; }

    public bool IsActive { get; set; } = true;
}

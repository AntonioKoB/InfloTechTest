using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UserManagement.Models;

public class User
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public string Forename { get; set; } = default!;
    public string Surname { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateOnly DateOfBirth { get; set; }
    // Never serialized: the audit log stores before/after snapshots of the whole User as JSON and renders
    // them as a diff, and a credential - hashed or not - has no business there. Null means no credential
    // has been set yet (e.g. rows that existed before the column did), which cannot be used to sign in.
    [JsonIgnore]
    public string? PasswordHash { get; set; }
}

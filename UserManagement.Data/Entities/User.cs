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
    // Never serialized: the audit log snapshots the whole User as JSON. Null means no credential has been
    // set.
    [JsonIgnore]
    public string? PasswordHash { get; set; }

    // The service-layer cache stores and returns copies, so a caller editing a fetched user cannot alter the
    // cached one.
    public User Clone() => (User)MemberwiseClone();
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.Models;

public class UserLog
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public long UserId { get; set; }
    public UserLogAction Action { get; set; }
    public DateTime Timestamp { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}

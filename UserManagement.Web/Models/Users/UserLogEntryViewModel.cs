using System;
using UserManagement.Models;

namespace UserManagement.Web.Models.Users;

public class UserLogEntryViewModel
{
    public long Id { get; set; }
    public UserLogAction Action { get; set; }
    public DateTime Timestamp { get; set; }
}

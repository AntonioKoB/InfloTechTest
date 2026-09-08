using System;
using UserManagement.Models;

namespace UserManagement.Web.Models.Logs;

public class LogListItemViewModel
{
    public long UserId { get; set; }
    public UserLogAction Action { get; set; }
    public DateTime Timestamp { get; set; }
}

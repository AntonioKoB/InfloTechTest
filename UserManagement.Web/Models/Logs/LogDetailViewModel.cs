using System;
using UserManagement.Models;

namespace UserManagement.Web.Models.Logs;

public class LogDetailViewModel
{
    public long UserId { get; set; }
    public UserLogAction Action { get; set; }
    public DateTime Timestamp { get; set; }
    public List<LogFieldChangeViewModel> Changes { get; set; } = [];
}

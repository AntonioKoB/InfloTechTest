namespace UserManagement.Web.Models.Logs;

public class LogFieldChangeViewModel
{
    public string PropertyName { get; set; } = default!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

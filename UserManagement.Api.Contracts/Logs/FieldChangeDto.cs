namespace UserManagement.Api.Contracts.Logs;

public class FieldChangeDto
{
    public string PropertyName { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

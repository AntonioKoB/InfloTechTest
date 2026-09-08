namespace UserManagement.Services.Domain;

public class FieldChange
{
    public required string PropertyName { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

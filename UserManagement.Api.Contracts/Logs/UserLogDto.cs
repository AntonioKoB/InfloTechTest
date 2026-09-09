namespace UserManagement.Api.Contracts.Logs;

public class UserLogDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public UserLogAction Action { get; set; }
    public DateTime Timestamp { get; set; }
    public IReadOnlyList<FieldChangeDto> Changes { get; set; } = [];
}

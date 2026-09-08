namespace UserManagement.Web.Models.Logs;

public class LogListViewModel
{
    public List<LogListItemViewModel> Items { get; set; } = [];
    public int Page { get; set; }
    public int TotalPages { get; set; }
}

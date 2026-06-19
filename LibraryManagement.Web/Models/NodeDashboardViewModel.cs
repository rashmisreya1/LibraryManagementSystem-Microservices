namespace LibraryManagement.Web.Models;

public class NodeDashboardViewModel
{
    public int TotalBooks { get; set; }

    public int TotalCopies { get; set; }

    public int HistoryCount { get; set; }

    public int ActiveBorrowings { get; set; }
}
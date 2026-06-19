namespace LibraryManagement.Web.Models;

public class DashboardStatsViewModel
{
    public int TotalBooks { get; set; }

    public int TotalCopies { get; set; }

    public int TotalUsers { get; set; }

    public int ActiveBorrowings { get; set; }
}
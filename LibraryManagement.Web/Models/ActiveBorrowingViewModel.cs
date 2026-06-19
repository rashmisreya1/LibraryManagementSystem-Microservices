namespace LibraryManagement.Web.Models;

public class ActiveBorrowingViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int UserId { get; set; }

    public DateTime IssueDate { get; set; }
}
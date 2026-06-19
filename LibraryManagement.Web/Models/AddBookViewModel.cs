using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Web.Models;

public class AddBookViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    [Display(Name = "Available Copies")]
    public int AvailableCopies { get; set; }
}
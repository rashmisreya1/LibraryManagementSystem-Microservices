using Microsoft.EntityFrameworkCore;
using LibraryManagement.Auth.API.Models;

namespace LibraryManagement.Auth.API.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    //public DbSet<Book> Books { get; set; }
    
    public DbSet<User> Users { get; set; }

    //public DbSet<IssueRecord> IssueRecords { get; set; }
}
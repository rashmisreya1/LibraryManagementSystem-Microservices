using LibraryManagement.Books.API.Data;
using LibraryManagement.Books.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using StackExchange.Redis;
using System.Text.Json;

using RabbitMQ.Client;
using System.Text;

namespace LibraryManagement.Books.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly LibraryDbContext _context;
    private readonly ILogger<BooksController> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly HttpClient _httpClient;

    public BooksController(
        LibraryDbContext context,
        ILogger<BooksController> logger,
        IConnectionMultiplexer redis,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _redis = redis;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Book>>> SearchBooks(string title)
    {
        _logger.LogInformation(
            "SearchBooks API called. Title: {Title}",
            title);

        var db = _redis.GetDatabase();

        string cacheKey = $"search_{title}";

        var cachedBooks = await db.StringGetAsync(cacheKey);

        if (!cachedBooks.IsNullOrEmpty)
        {
            _logger.LogInformation(
                "Search results found in Redis cache.");

            var booksFromCache =
                JsonSerializer.Deserialize<List<Book>>(cachedBooks.ToString());

            return booksFromCache!;
        }

        _logger.LogInformation(
            "Search results not found in cache. Fetching from database.");

        var books = await _context.Books
            .Where(b => b.Title.Contains(title))
            .ToListAsync();

        await db.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(books),
            TimeSpan.FromMinutes(10));

        _logger.LogInformation(
            "Search results stored in Redis cache.");

        return books;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {

        _logger.LogInformation("GetStats API called");

        var totalBooks = await _context.Books.CountAsync();

        var totalCopies = await _context.Books
            .SumAsync(b => b.AvailableCopies);

        //var totalUsers = await _context.Users.CountAsync();

        var activeBorrowings = await _context.IssueRecords
            .CountAsync(r => r.ReturnDate == null);

        return Ok(new
        {
            TotalBooks = totalBooks,
            TotalCopies = totalCopies,
            //TotalUsers = totalUsers,
            ActiveBorrowings = activeBorrowings
        });
    }

    [HttpPost("issue/{id}")]
    public async Task<IActionResult> IssueBook(int id)
    {

        _logger.LogInformation(
            "IssueBook API called. BookId: {BookId}",
            id);


        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            _logger.LogWarning(
                "Issue failed. Book not found. BookId: {BookId}",
                id);

            return NotFound();
        }

        if (book.AvailableCopies <= 0)
        {
            _logger.LogWarning(
                "Issue failed. No copies available. BookId: {BookId}",
                id);

            return BadRequest("No copies available");
        }

        book.AvailableCopies--;

        var issueRecord = new IssueRecord
        {
            BookId = id,
            UserId = 1,
            IssueDate = DateTime.Now
        };

        _context.IssueRecords.Add(issueRecord);

        await _context.SaveChangesAsync();

        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: "book_issue_queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        string message = $"Book with Id {id} issued successfully";

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: "",
            routingKey: "book_issue_queue",
            basicProperties: null,
            body: body);

        _logger.LogInformation(
            "RabbitMQ message sent for issued book. BookId: {BookId}",
            id);

        var db = _redis.GetDatabase();

        await db.KeyDeleteAsync($"book_{id}");

        _logger.LogInformation(
            "Book cache removed after issue. BookId: {BookId}",
            id);

        _logger.LogInformation(
            "Book issued successfully. BookId: {BookId}",
            id);

        return Ok(book);
    }

    [HttpPost("return/{id}")]
    public async Task<IActionResult> ReturnBook(int id)
    {

        _logger.LogInformation(
            "ReturnBook API called. BookId: {BookId}",
            id);

        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            _logger.LogWarning(
                "Return failed. Book not found. BookId: {BookId}",
                id);

            return NotFound();
        }

        book.AvailableCopies++;

        var issueRecord = await _context.IssueRecords
            .Where(r => r.BookId == id && r.ReturnDate == null)
            .OrderByDescending(r => r.IssueDate)
            .FirstOrDefaultAsync();

        if (issueRecord != null)
        {
            issueRecord.ReturnDate = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: "book_return_queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        string message = $"Book with Id {id} returned successfully";

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: "",
            routingKey: "book_return_queue",
            basicProperties: null,
            body: body);

        _logger.LogInformation(
            "RabbitMQ message sent for returned book. BookId: {BookId}",
            id);

        var db = _redis.GetDatabase();

        await db.KeyDeleteAsync($"book_{id}");

        _logger.LogInformation(
            "Book cache removed after return. BookId: {BookId}",
            id);

        _logger.LogInformation(
            "Book returned successfully. BookId: {BookId}",
            id);

        return Ok(book);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
    {
        _logger.LogInformation("GetBooks API called");

        return await _context.Books.ToListAsync();
    }

    [HttpGet("authors")]
    public async Task<ActionResult<IEnumerable<string>>> GetAuthors()
    {
        _logger.LogInformation("GetAuthors API called");

        var db = _redis.GetDatabase();

        string cacheKey = "authors";

        var cachedAuthors = await db.StringGetAsync(cacheKey);

        if (!cachedAuthors.IsNullOrEmpty)
        {
            _logger.LogInformation(
                "Authors found in Redis cache");

            var authorsFromCache =
                JsonSerializer.Deserialize<List<string>>(cachedAuthors.ToString());

            return authorsFromCache!;
        }

        _logger.LogInformation(
            "Authors not found in cache. Fetching from database.");

        var authors = await _context.Books
            .Select(b => b.Author)
            .Distinct()
            .ToListAsync();

        await db.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(authors),
            TimeSpan.FromMinutes(10));

        _logger.LogInformation(
            "Authors stored in Redis cache");

        return authors;
    }

    [HttpPost]
    public async Task<ActionResult<Book>> AddBook(Book book)
    {
        _logger.LogInformation(
            "AddBook API called. Title: {Title}",
            book.Title);

        _context.Books.Add(book);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Book added successfully. Id: {Id}, Title: {Title}",
            book.Id,
            book.Title);

        return CreatedAtAction(
            nameof(GetBooks),
            new { id = book.Id },
            book);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        _logger.LogInformation(
            "DeleteBook API called. Id: {Id}",
            id);

        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            _logger.LogWarning(
                "Delete failed. Book not found. Id: {Id}",
                id);

            return NotFound();
        }

        _context.Books.Remove(book);

        await _context.SaveChangesAsync();

        var db = _redis.GetDatabase();

        await db.KeyDeleteAsync($"book_{id}");

        _logger.LogInformation(
            "Book cache removed after delete. Id: {Id}",
            id);

        _logger.LogInformation(
            "Book deleted successfully. Id: {Id}",
            id);

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBook(int id, Book updatedBook)
    {

        _logger.LogInformation(
            "UpdateBook API called. Id: {Id}",
            id);


        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            _logger.LogWarning(
                "Update failed. Book not found. Id: {Id}",
                id);

            return NotFound();
        }

        book.Title = updatedBook.Title;
        book.Author = updatedBook.Author;
        book.AvailableCopies = updatedBook.AvailableCopies;

        await _context.SaveChangesAsync();

        var db = _redis.GetDatabase();

        await db.KeyDeleteAsync($"book_{id}");

        _logger.LogInformation(
            "Book cache removed after update. Id: {Id}",
            id);

        _logger.LogInformation(
            "Book updated successfully. Id: {Id}",
            id);

        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Book>> GetBook(int id)
    {
        _logger.LogInformation(
            "GetBook API called. Id: {Id}",
            id);

        var db = _redis.GetDatabase();

        string cacheKey = $"book_{id}";

        var cachedBook = await db.StringGetAsync(cacheKey);

        if (!cachedBook.IsNullOrEmpty)
        {
            _logger.LogInformation(
                "Book found in Redis cache. Id: {Id}",
                id);

            var bookFromCache =
                JsonSerializer.Deserialize<Book>(cachedBook.ToString());

            return bookFromCache!;
        }

        _logger.LogInformation(
            "Book not found in cache. Fetching from database. Id: {Id}",
            id);

        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            _logger.LogWarning(
                "Book not found. Id: {Id}",
                id);

            return NotFound();
        }

        await db.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(book),
            TimeSpan.FromMinutes(10));

        _logger.LogInformation(
            "Book stored in Redis cache. Id: {Id}",
            id);

        return book;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        _logger.LogInformation("GetHistory API called");

        var history = await _context.IssueRecords
            .OrderByDescending(r => r.IssueDate)
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} history records",
            history.Count);

        return Ok(history);
    }

    [HttpGet("activeborrowings")]
    public async Task<IActionResult> GetActiveBorrowings()
    {
        _logger.LogInformation("GetActiveBorrowings API called");

        var db = _redis.GetDatabase();

        string cacheKey = "activeborrowings";

        var cachedRecords = await db.StringGetAsync(cacheKey);

        if (!cachedRecords.IsNullOrEmpty)
        {
            _logger.LogInformation(
                "Active borrowings found in Redis cache");

            return Ok(
                JsonSerializer.Deserialize<object>(
                    cachedRecords.ToString()));
        }

        _logger.LogInformation(
            "Active borrowings not found in cache. Fetching from database.");

        var records = await _context.IssueRecords
            .Where(r => r.ReturnDate == null)
            .OrderByDescending(r => r.IssueDate)
            .ToListAsync();

        await db.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(records),
            TimeSpan.FromMinutes(10));

        _logger.LogInformation(
            "Active borrowings stored in Redis cache");

        return Ok(records);
    }

    [HttpGet("topissued")]
    public async Task<IActionResult> GetTopIssuedBooks()
    {
        _logger.LogInformation("GetTopIssuedBooks API called");

        var db = _redis.GetDatabase();

        string cacheKey = "topissued";

        var cachedTopBooks = await db.StringGetAsync(cacheKey);

        if (!cachedTopBooks.IsNullOrEmpty)
        {
            _logger.LogInformation(
                "Top issued books found in Redis cache");

            return Ok(
                JsonSerializer.Deserialize<object>(
                    cachedTopBooks.ToString()));
        }

        _logger.LogInformation(
            "Top issued books not found in cache. Fetching from database.");

        var topBooks = await _context.IssueRecords
            .GroupBy(r => r.BookId)
            .Select(g => new
            {
                BookId = g.Key,
                IssueCount = g.Count()
            })
            .OrderByDescending(x => x.IssueCount)
            .ToListAsync();

        await db.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(topBooks),
            TimeSpan.FromMinutes(10));

        _logger.LogInformation(
            "Top issued books stored in Redis cache");

        return Ok(topBooks);
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryStatus()
    {
        var response = await _httpClient.GetAsync(
            "http://localhost:5174/api/Inventory/status");

        var content = await response.Content.ReadAsStringAsync();

        return Content(content);
    }
}


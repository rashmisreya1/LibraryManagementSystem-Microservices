using LibraryManagement.Users.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly LibraryDbContext _context;
    private readonly ILogger<UsersController> _logger;
    private readonly HttpClient _httpClient;

    public UsersController(
        LibraryDbContext context,
        ILogger<UsersController> logger,
        IHttpClientFactory httpClientFactory)

    {
        _context = context;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("{id}/issuedbooks")]
    public async Task<IActionResult> GetIssuedBooks(int id)
    {
        _logger.LogInformation(
            "GetIssuedBooks API called. UserId: {UserId}",
            id);

        var records = await _context.IssueRecords
            .Where(r => r.UserId == id)
            .OrderByDescending(r => r.IssueDate)
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} issued books for UserId: {UserId}",
            records.Count,
            id);

        return Ok(records);
    }

    [HttpGet("notification")]
    public async Task<IActionResult> GetNotificationStatus()
    {
        var response = await _httpClient.GetAsync(
            "http://localhost:5260/api/Notification/status");

        var content = await response.Content.ReadAsStringAsync();

        return Content(content);
    }
}
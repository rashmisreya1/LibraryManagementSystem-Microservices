using LibraryManagement.Auth.API.Data;
using LibraryManagement.Auth.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Auth.API.Controllers;

using RabbitMQ.Client;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LibraryDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly HttpClient _httpClient;

    public AuthController(
        LibraryDbContext context,
        ILogger<AuthController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }   

    [HttpPost("signup")]
    public async Task<ActionResult<User>> Signup(User user)
    {
        _logger.LogInformation(
            "Signup API called. Email: {Email}",
            user.Email);

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        using var connection = factory.CreateConnection();

        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: "user_registration_queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        string message =
            $"New user registered with email {user.Email}";

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: "",
            routingKey: "user_registration_queue",
            basicProperties: null,
            body: body);

        _logger.LogInformation(
            "RabbitMQ message sent for user registration. Email: {Email}",
            user.Email);

        _logger.LogInformation(
            "User signed up successfully. Email: {Email}",
            user.Email);

        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login(User loginUser)
    {
        _logger.LogInformation(
            "Login API called. Email: {Email}",
            loginUser.Email);

        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.Email == loginUser.Email &&
                u.Password == loginUser.Password);

        if (user == null)
        {
            _logger.LogWarning(
                "Login failed. Invalid credentials for Email: {Email}",
                loginUser.Email);

            return Unauthorized("Invalid email or password");
        }

        _logger.LogInformation(
            "Login successful. Email: {Email}",
            loginUser.Email);

        return Ok(user);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsersNotificationStatus()
    {
        var response = await _httpClient.GetAsync(
            "http://localhost:5150/api/Users/notification");

        var content = await response.Content.ReadAsStringAsync();

        return Content(content);
    }
}
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Gateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public GatewayController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("books")]
    public async Task<IActionResult> GetBooks()
    {
        var response = await _httpClient.GetAsync(
            "http://localhost:5230/api/Books");

        var content = await response.Content.ReadAsStringAsync();

        return Content(content, "application/json");
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryStatus()
    {
        var response = await _httpClient.GetAsync(
            "http://localhost:5230/api/Books/inventory");

        var content = await response.Content.ReadAsStringAsync();

        return Content(content);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsersStatus()
    {
        var response = await _httpClient.GetAsync(
            "http://localhost:5213/api/Auth/users");

        var content = await response.Content.ReadAsStringAsync();

        return Content(content);
    }
}
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public InventoryController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var response = await _httpClient.GetAsync(
            "http://localhost:5150/api/Users/notification");

        var content = await response.Content.ReadAsStringAsync();

        return Content(
            $"Inventory API reached successfully → {content}");
    }
}
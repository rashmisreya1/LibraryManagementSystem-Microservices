using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Notification.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok("Notification API reached successfully");
    }
}
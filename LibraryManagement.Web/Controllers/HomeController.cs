using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Web.Models;
using System.Net.Http.Json;

namespace LibraryManagement.Web.Controllers;

public class HomeController : Controller
{

    public IActionResult PortalSelection()
    {
        return View();
    }

    public async Task<IActionResult> Index()
    {
        using var client = new HttpClient();

        var stats = await client.GetFromJsonAsync<DashboardStatsViewModel>(
            "http://localhost:5230/api/books/stats");

        return View(stats);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

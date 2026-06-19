using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using LibraryManagement.Web.Models;

namespace LibraryManagement.Web.Controllers;

public class NodeAccountController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(NodeLoginRequest request)
    {
        using var client = new HttpClient();

        var response = await client.PostAsJsonAsync(
            "http://localhost:5213/api/auth/login",
            request);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Login Successful";
            return RedirectToAction(
                "Dashboard",
                "NodeBooks");
        }

        ViewBag.Error = "Invalid Credentials";

        return View(request);
    }

    [HttpGet]
    public IActionResult Signup()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Signup(NodeSignupRequest request)
    {
        using var client = new HttpClient();

        var response = await client.PostAsJsonAsync(
            "http://localhost:5213/api/auth/signup",
            request);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Login));
        }

        ViewBag.Error = "Signup Failed";

        return View(request);
    }
}
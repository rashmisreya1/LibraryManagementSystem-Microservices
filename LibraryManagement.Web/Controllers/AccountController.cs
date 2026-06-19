using LibraryManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace LibraryManagement.Web.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        using var client = new HttpClient();

        var response = await client.PostAsJsonAsync(
            "http://localhost:5213/api/auth/login",
            request);

        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadFromJsonAsync<User>();

            HttpContext.Session.SetString(
                "Username",
                user!.Name);

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Message = "Invalid Credentials";

        return View();
    }

    [HttpGet]
    public IActionResult Signup()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Signup(SignupRequest request)
    {
        using var client = new HttpClient();

        var response = await client.PostAsJsonAsync(
            "http://localhost:5213/api/auth/signup",
            request);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Login));
        }

        ViewBag.Message = "Signup Failed";

        return View(request);
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction("Login");
    }
}
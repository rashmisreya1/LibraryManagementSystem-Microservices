using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using LibraryManagement.Web.Models;

namespace LibraryManagement.Web.Controllers;

public class NodeBooksController : Controller
{

    public async Task<IActionResult> Dashboard()
    {
        using var client = new HttpClient();

        var stats = await client.GetFromJsonAsync<DashboardStatsViewModel>(
            "http://localhost:5230/api/books/stats");

        var history = await client.GetFromJsonAsync<List<NodeIssueHistoryViewModel>>(
            "http://localhost:5230/api/books/history");

        var active = await client.GetFromJsonAsync<List<ActiveBorrowingViewModel>>(
            "http://localhost:5230/api/books/activeborrowings");

        var model = new NodeDashboardViewModel
        {
            TotalBooks = stats?.TotalBooks ?? 0,
            TotalCopies = stats?.TotalCopies ?? 0,
            HistoryCount = history?.Count ?? 0,
            ActiveBorrowings = active?.Count ?? 0
        };

        return View(model);
    }

    public async Task<IActionResult> TopIssued()
    {
        using var client = new HttpClient();

        var books = await client.GetFromJsonAsync<List<NodeTopIssuedBookViewModel>>(
            "http://localhost:5230/api/books/topissued");

        return View(books);
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        using var client = new HttpClient();

        List<BookViewModel>? books;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            books = await client.GetFromJsonAsync<List<BookViewModel>>(
                $"http://localhost:5230/api/books/search?title={searchTerm}");
        }
        else
        {
            books = await client.GetFromJsonAsync<List<BookViewModel>>(
                "http://localhost:5230/api/books");
        }

        ViewBag.SearchTerm = searchTerm;

        return View(books);
    }

    public async Task<IActionResult> History()
    {
        using var client = new HttpClient();

        var history = await client.GetFromJsonAsync<List<NodeIssueHistoryViewModel>>(
            "http://localhost:5230/api/books/history");

        return View(history);
    }

    public async Task<IActionResult> ActiveBorrowings()
    {
        using var client = new HttpClient();

        var books = await client.GetFromJsonAsync<List<ActiveBorrowingViewModel>>(
            "http://localhost:5230/api/books/activeborrowings");

        return View(books);
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Add(BookViewModel book)
    {
        using var client = new HttpClient();

        var response = await client.PostAsJsonAsync(
            "http://localhost:5230/api/books",
            new
            {
                title = book.Title,
                author = book.Author,
                availableCopies = book.AvailableCopies
            });

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        using var client = new HttpClient();

        var book = await client.GetFromJsonAsync<BookViewModel>(
            $"http://localhost:5230/api/books/{id}");

        return View(book);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(BookViewModel book)
    {
        using var client = new HttpClient();

        var response = await client.PutAsJsonAsync(
            $"http://localhost:5230/api/books/{book.Id}",
            new
            {
                title = book.Title,
                author = book.Author,
                availableCopies = book.AvailableCopies
            });

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }


    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        using var client = new HttpClient();

        var book = await client.GetFromJsonAsync<BookViewModel>(
            $"http://localhost:5230/api/books/{id}");

        return View(book);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(BookViewModel book)
    {
        using var client = new HttpClient();

        var response = await client.DeleteAsync(
            $"http://localhost:5230/api/books/{book.Id}");

        return RedirectToAction(nameof(Index));
    }
}
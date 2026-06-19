using LibraryManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace LibraryManagement.Web.Controllers;

public class BooksController : Controller
{
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

    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddBookViewModel book)
    {
        using var client = new HttpClient();

        var response = await client.PostAsJsonAsync(
            "http://localhost:5230/api/books",
            book);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        using var client = new HttpClient();

        var response = await client.DeleteAsync(
            $"http://localhost:5230/api/books/{id}");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Issue(int id)
    {
        using var client = new HttpClient();

        await client.PostAsync(
            $"http://localhost:5230/api/books/issue/{id}",
            null);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Return(int id)
    {
        using var client = new HttpClient();

        await client.PostAsync(
            $"http://localhost:5230/api/books/return/{id}",
            null);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        using var client = new HttpClient();

        var books = await client.GetFromJsonAsync<List<BookViewModel>>(
            "http://localhost:5230/api/books");

        var book = books?.FirstOrDefault(b => b.Id == id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(BookViewModel book)
    {
        using var client = new HttpClient();

        var response = await client.PutAsJsonAsync(
            $"http://localhost:5230/api/books/{book.Id}",
            book);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        using var client = new HttpClient();

        var history = await client.GetFromJsonAsync<List<IssueRecordViewModel>>(
            "http://localhost:5230/api/books/history");

        return View(history);
    }

    [HttpGet]
    public async Task<IActionResult> TopIssued()
    {
        using var client = new HttpClient();

        var books = await client.GetFromJsonAsync<List<TopIssuedBookViewModel>>(
             "http://localhost:5230/api/books/topissued");

        return View(books);
    }
}
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using devjamin.Models;
using BlogSite.Models;
using Microsoft.EntityFrameworkCore;

namespace devjamin.Controllers;

public class HomeController : Controller
{
    private readonly BlogDbContext _context;

    public HomeController(BlogDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var latestBlogs = await _context.BlogPosts
            .Where(b => b.IsPublished) // Only show published blogs
            .OrderByDescending(b => b.PublishedDate)
            .Take(3)
            .ToListAsync();

        return View(latestBlogs);
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

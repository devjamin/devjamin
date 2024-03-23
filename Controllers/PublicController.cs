/*
    PublicController.cs, manages public facing functionality.

    Admin panel access:
    - GET: /
    - GET: /Blogs
    - GET: /Blog/[SLUG]
*/
using Microsoft.AspNetCore.Mvc;
using devjamin.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace devjamin.Controllers
{
  [Route("")]
  public class PublicController(BlogDbContext context) : Controller
  {
    private readonly BlogDbContext _context = context;

    // GET: /
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
      var latestThreePublishedBlogs = await _context.BlogPosts
          .Where(b => b.IsPublished)
          .OrderByDescending(b => b.PublishedDate)
          .Take(3)
          .ToListAsync();

      return View("~/Views/Home/Index.cshtml", latestThreePublishedBlogs);
    }

    // GET: /Blogs
    [HttpGet("/Blogs")]
    public async Task<IActionResult> Blogs()
    {
      var allPublishedBlogs = await _context.BlogPosts
          .Where(b => b.IsPublished)
          .OrderByDescending(b => b.PublishedDate)
          .ToListAsync();

      return View("~/Views/Blog/Blogs.cshtml", allPublishedBlogs);
    }

    // GET: /Blog/{Slug}
    [HttpGet("/Blog/{slug}")]
    public async Task<IActionResult> Post(string slug)
    {
      BlogPost? blogPost = null;

      // First try to get the published post
      blogPost = await _context.BlogPosts
          .Include(bp => bp.Author)
          .FirstOrDefaultAsync(bp => bp.Slug == slug && bp.IsPublished);

      // If not found and user is logged in, try to get unpublished post (so admin can preview it)
      if (blogPost == null && User?.Identity?.IsAuthenticated == true)
      {
        blogPost = await _context.BlogPosts
          .Include(bp => bp.Author)
          .FirstOrDefaultAsync(bp => bp.Slug == slug && !bp.IsPublished);
      }

      if (blogPost == null)
      {
        return NotFound();
      }

      return View("~/Views/Blog/Blog.cshtml", blogPost);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}

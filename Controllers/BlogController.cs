using Microsoft.AspNetCore.Mvc;
using devjamin.Models;
using Microsoft.EntityFrameworkCore;

namespace devjamin.Controllers
{
  public class BlogController : Controller
  {
    private readonly BlogDbContext _context;

    public BlogController(BlogDbContext context)
    {
      _context = context;
    }

    public IActionResult Index()
    {
      return View("Index");
    }

    // GET: /Blog/[SLUG]
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

      return View("Blog", blogPost);
    }
  }
}

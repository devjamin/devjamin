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
    public class PublicController(BlogDbContext context, IConfiguration configuration) : Controller
    {
        private readonly BlogDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;

        // GET: /
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var latestThreePublishedBlogs = await _context.BlogPosts
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.PublishedDate)
                .Take(3)
                .Select(blog => new BlogPostCardViewModel
                {
                    BlogPostId = blog.BlogPostId,
                    Title = blog.Title,
                    Slug = blog.Slug,
                    Description = blog.Description
                })
                .ToListAsync();

            return View("~/Views/Public/Index.cshtml", latestThreePublishedBlogs);
        }

        // GET: /Blogs
        [HttpGet("/Blogs")]
        public async Task<IActionResult> Blogs()
        {
            var allPublishedBlogs = await _context.BlogPosts
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.PublishedDate)
                .ToListAsync();

            return View("~/Views/Public/BlogsListing.cshtml", allPublishedBlogs);
        }

        // GET: /Blog/{Slug}
        [HttpGet("/Blog/{slug}")]
        public async Task<IActionResult> GetSpecificBlog(string slug)
        {
            var blogPostViewModel = await _context.BlogPosts
              .Where(bp => bp.Slug == slug && (bp.IsPublished || (User != null && User.Identity != null && User.Identity.IsAuthenticated)))
              .Select(bp => new BlogPostViewModel
              {
                  BlogPostId = bp.BlogPostId,
                  Title = bp.Title,
                  Slug = bp.Slug,
                  Description = bp.Description,
                  PublishedDate = bp.PublishedDate,
                  Content = bp.Content,
                  AuthorFullName = bp.Author!.FullName, // Author is always present
                  AuthorProfilePicturePath = bp.Author.ProfilePicturePath,
                  ReadTimeMinuteEstimate = CalculateReadTime(bp.Content)
              })
              .FirstOrDefaultAsync();

            // Add CDN base path
            ViewData["CDNBASEPATH"] = _configuration.GetValue<string>("EnvironmentVariables:CDNBASEPATH");

            if (blogPostViewModel == null)
            {
                return NotFound();
            }

            return View("~/Views/Public/FullBlog.cshtml", blogPostViewModel);
        }

        private static int CalculateReadTime(string content)
        {
            int averageWordReadSpeedPerMinute = 200;
            int wordCount = content.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            int readTimeMinutes = (int)Math.Ceiling(wordCount / (double)averageWordReadSpeedPerMinute);

            // Ensure at least 1 minute is returned for very short texts
            return Math.Max(readTimeMinutes, 1);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

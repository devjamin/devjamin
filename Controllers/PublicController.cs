/*
    PublicController.cs, manages public facing functionality.

    Admin panel access:
    - GET: /
    - GET: /Articles
    - GET: /Article/[SLUG]
*/
using Microsoft.AspNetCore.Mvc;
using devjamin.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace devjamin.Controllers
{
    [Route("")]
    public class PublicController(BlogDbContext context, IConfiguration configuration, IMemoryCache cache) : Controller
    {
        private readonly BlogDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _cache = cache;

        // GET: /
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            // Demo of memory caching, I want to save for later
            // // Define cache key
            // string cacheKey = "LatestThreePublishedBlogs";

            // // Try to retrieve the data from cache
            // if (!_cache.TryGetValue(cacheKey, out List<BlogPostCardViewModel>? latestThreePublishedBlogs))
            // {
            //     latestThreePublishedBlogs = await _context.BlogPosts
            //         .Where(b => b.IsPublished)
            //         .OrderByDescending(b => b.CreatedDate)
            //         .Take(3)
            //         .Select(blog => new BlogPostCardViewModel
            //         {
            //             BlogPostId = blog.BlogPostId,
            //             Title = blog.Title,
            //             Slug = blog.Slug,
            //             Description = blog.Description,
            //             Tags = blog.Tags.Select(tag => new BlogTagViewModel
            //             {
            //                 Name = tag.Name // Only need the name for the blog card
            //             }).ToList()
            //         })
            //         .ToListAsync();

            //     // Set cache options
            //     var cacheEntryOptions = new MemoryCacheEntryOptions()
            //         .SetSlidingExpiration(TimeSpan.FromMinutes(5));

            //     // Save data in cache
            //     _cache.Set(cacheKey, latestThreePublishedBlogs, cacheEntryOptions);
            // }

            var latestThreePublishedBlogs = await _context.BlogPosts
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.CreatedDate)
                .Include(b => b.Media)
                .Take(3)
                .Select(blog => new BlogPostCardViewModel
                {
                    BlogPostId = blog.BlogPostId,
                    Title = blog.Title,
                    Slug = blog.Slug,
                    Description = blog.Description,
                    Tags = blog.Tags.Select(tag => new BlogTagViewModel
                    {
                        Name = tag.Name // Only need the name for the blog card
                    }).ToList(),
                    CoverImagePath = blog.Media.FirstOrDefault(m => m.IsCoverImage)!.FilePath
                })
                .ToListAsync();

            return View("~/Views/Public/Index.cshtml", latestThreePublishedBlogs);
        }

        // GET: /Articles
        [HttpGet("/Articles")]
        public async Task<IActionResult> Blogs()
        {
            var allPublishedBlogs = await _context.BlogPosts
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.CreatedDate)
                .Include(b => b.Media)
                .Select(blog => new BlogPostCardViewModel
                {
                    BlogPostId = blog.BlogPostId,
                    Title = blog.Title,
                    Slug = blog.Slug,
                    Description = blog.Description,
                    Tags = blog.Tags.Select(tag => new BlogTagViewModel
                    {
                        Name = tag.Name
                    }).ToList(),
                    CoverImagePath = blog.Media.FirstOrDefault(m => m.IsCoverImage)!.FilePath
                })
                .ToListAsync();

            return View("~/Views/Public/BlogsListing.cshtml", allPublishedBlogs);
        }

        // GET: /Article/{Slug}
        [HttpGet("/Article/{slug}")]
        public async Task<IActionResult> GetSpecificBlog(string slug)
        {
            var blogPostViewModel = await _context.BlogPosts
              .Where(bp => bp.Slug == slug && (bp.IsPublished || (User != null && User.Identity != null && User.Identity.IsAuthenticated)))
              .Include(bp => bp.Author)
              .Include(bp => bp.Media)
              .Select(bp => new BlogPostViewModel
              {
                  BlogPostId = bp.BlogPostId,
                  Title = bp.Title,
                  Slug = bp.Slug,
                  Description = bp.Description,
                  CreatedDate = bp.CreatedDate,
                  Content = bp.Content,
                  AuthorFullName = bp.Author!.FullName, // Author is always present
                  AuthorProfilePicturePath = bp.Author.ProfilePicturePath,
                  ReadTimeMinuteEstimate = CalculateReadTime(bp.Content),
                  CoverImagePath = bp.Media.FirstOrDefault(m => m.IsCoverImage)!.FilePath,
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

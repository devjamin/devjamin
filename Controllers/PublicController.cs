/*
    PublicController.cs, manages public facing functionality.

    Public facing routes:
    - GET: /
    - GET: /Articles
    - GET: /Article/[SLUG]

    Legacy redirects (kept for SEO of pre-rename URLs):
    - GET: /Blogs    -> 301 /Articles
    - GET: /Blog/[SLUG] -> 301 /Article/[SLUG]
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
            var latestThreePublishedArticles = await _context.BlogPosts
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.CreatedDate)
                .Include(b => b.Media)
                .Take(3)
                .Select(article => new ArticleCardViewModel
                {
                    ArticleId = article.BlogPostId,
                    Title = article.Title,
                    Slug = article.Slug,
                    Description = article.Description,
                    Tags = article.Tags.Select(tag => new ArticleTagViewModel
                    {
                        Name = tag.Name
                    }).ToList(),
                    CoverImagePath = article.Media.FirstOrDefault(m => m.IsCoverImage)!.FilePath
                })
                .ToListAsync();

            return View("~/Views/Public/Index.cshtml", latestThreePublishedArticles);
        }

        // GET: /Articles
        [HttpGet("/Articles")]
        public async Task<IActionResult> Articles()
        {
            var allPublishedArticles = await _context.BlogPosts
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.CreatedDate)
                .Include(b => b.Media)
                .Select(article => new ArticleCardViewModel
                {
                    ArticleId = article.BlogPostId,
                    Title = article.Title,
                    Slug = article.Slug,
                    Description = article.Description,
                    Tags = article.Tags.Select(tag => new ArticleTagViewModel
                    {
                        Name = tag.Name
                    }).ToList(),
                    CoverImagePath = article.Media.FirstOrDefault(m => m.IsCoverImage)!.FilePath
                })
                .ToListAsync();

            return View("~/Views/Public/ArticlesListing.cshtml", allPublishedArticles);
        }

        // GET: /Blogs (legacy redirect)
        [HttpGet("/Blogs")]
        public IActionResult BlogsRedirect()
        {
            return RedirectPermanent("/Articles");
        }

        // GET: /Article/{Slug}
        [HttpGet("/Article/{slug}")]
        public async Task<IActionResult> GetSpecificArticle(string slug)
        {
            // First try to find the article with current slug
            var article = await _context.BlogPosts
                .Where(bp => bp.Slug == slug && (bp.IsPublished || (User != null && User.Identity != null && User.Identity.IsAuthenticated)))
                .FirstOrDefaultAsync();

            // If not found, check slug history
            if (article == null)
            {
                var slugHistory = await _context.BlogPostSlugHistory
                    .Include(sh => sh.BlogPost)
                    .FirstOrDefaultAsync(sh => sh.OldSlug == slug);

                if (slugHistory != null && slugHistory.BlogPost.IsPublished)
                {
                    // Permanent redirect to the new slug
                    return RedirectPermanent($"/Article/{slugHistory.BlogPost.Slug}");
                }

                return NotFound();
            }

            var articleViewModel = await _context.BlogPosts
                .Where(bp => bp.BlogPostId == article.BlogPostId)
                .Include(bp => bp.Author)
                .Include(bp => bp.Media)
                .Select(bp => new ArticleViewModel
                {
                    ArticleId = bp.BlogPostId,
                    Title = bp.Title,
                    Slug = bp.Slug,
                    Description = bp.Description,
                    MetaDescription = bp.MetaDescription,
                    CreatedDate = bp.CreatedDate,
                    UpdatedDate = bp.EditedDate,
                    Content = bp.SemanticHTML,
                    AuthorFullName = bp.Author!.FullName,
                    AuthorProfilePicturePath = bp.Author.ProfilePicturePath,
                    ReadTimeMinuteEstimate = CalculateReadTime(bp.SemanticHTML),
                    CoverImagePath = bp.Media.FirstOrDefault(m => m.IsCoverImage)!.FilePath,
                })
                .FirstOrDefaultAsync();

            // Add CDN base path
            ViewData["CDNBASEPATH"] = _configuration.GetValue<string>("EnvironmentVariables:CDNBASEPATH");

            if (articleViewModel == null)
            {
                return NotFound();
            }

            return View("~/Views/Public/FullArticle.cshtml", articleViewModel);
        }

        // GET: /Blog/{Slug} (legacy redirect)
        [HttpGet("/Blog/{slug}")]
        public IActionResult BlogRedirect(string slug)
        {
            return RedirectPermanent($"/Article/{slug}");
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

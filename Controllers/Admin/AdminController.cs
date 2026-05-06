/*
    AdminController.cs, manages admin panel functionality.

    Admin panel access:
    - GET: /Admin (anonymous)
    - GET & POST: /Admin/Login (anonymous)
    - GET: /Admin/Logout

    Articles management:
    - GET: /Admin/Articles
    - GET & POST: /Admin/Articles/Create
    - GET & POST: /Admin/Articles/Edit/[ArticleId]
    - GET: /Admin/Articles/Delete/[ArticleId]
*/
using devjamin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace devjamin.Controllers
{
    [Route("Admin")]
    [Authorize]
    public class AdminController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, BlogDbContext context, IConfiguration configuration) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly BlogDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;

        // GET: /Admin
        [HttpGet("")]
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Articles", "Admin");
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }
        }

        // GET: Admin/Login
        [HttpGet("Login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            var adminLoginViewModel = new AdminLoginViewModel()
            {
                Username = "",
                Password = ""
            };
            return View("~/Views/Admin/Login.cshtml", adminLoginViewModel);
        }

        // POST: /Admin/Login
        [HttpPost("Login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel adminLoginViewModel)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(adminLoginViewModel.Username, adminLoginViewModel.Password, true, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Articles", "Admin");
                }

                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View("~/Views/Admin/Login.cshtml", adminLoginViewModel);
                }
            }

            return View("~/Views/Admin/Login.cshtml", adminLoginViewModel);
        }

        // GET: Admin/Articles
        [HttpGet("Articles")]
        public async Task<IActionResult> Articles()
        {
            var publishedArticles = await _context.BlogPosts
                .Where(b => b.IsPublished && !b.IsDraft)
                .OrderByDescending(b => b.EditedDate)
                .Include(b => b.Media)
                .Select(b => new ArticleCardViewModel
                {
                    ArticleId = b.BlogPostId,
                    Title = b.Title,
                    Slug = b.Slug,
                    Description = b.Description,
                    Tags = b.Tags.Select(t => new ArticleTagViewModel
                    {
                        Name = t.Name
                    }).ToList(),
                    CoverImagePath = b.Media.FirstOrDefault(m => m.IsCoverImage)!.FilePath
                })
                .ToListAsync();

            var nonPublishedArticles = await _context.BlogPosts
                .Where(b => !b.IsPublished && !b.IsDraft)
                .OrderByDescending(b => b.EditedDate)
                .Select(b => new ArticleCardViewModel
                {
                    ArticleId = b.BlogPostId,
                    Title = b.Title,
                    Slug = b.Slug,
                    Description = b.Description,
                    Tags = b.Tags.Select(t => new ArticleTagViewModel
                    {
                        Name = t.Name
                    }).ToList(),
                    CoverImagePath = b.Media.FirstOrDefault(m => m.IsCoverImage)!.FilePath
                })
                .ToListAsync();

            var articlesViewModel = new AllArticlesCardViewModel
            {
                PublishedArticles = publishedArticles,
                NonPublishedArticles = nonPublishedArticles
            };

            return View("~/Views/Admin/Articles/Index.cshtml", articlesViewModel);
        }


        // GET: Admin/Articles/Create
        // Create a new empty article in the background, then redirect to the create page
        // Upon form submission, the article will be modified
        [HttpGet("Articles/Create")]
        public async Task<IActionResult> Create()
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // create new empty article
            var DateNow = DateTime.UtcNow;
            var article = new BlogPost
            {
                Title = "",
                Slug = "",
                Description = "",
                MetaDescription = "",
                SemanticHTML = "",
                AuthorId = currentUser.Id,
                CreatedDate = DateNow,
                EditedDate = DateNow,
                IsPublished = false
            };

            _context.BlogPosts.Add(article);
            await _context.SaveChangesAsync();

            if (article == null)
            {
                return NotFound();
            }

            // Fetch all tags and prepare the view models for available and selected tags
            var allTags = await _context.BlogTags.ToListAsync();
            var availableTags = allTags.Select(t => new ArticleTagViewModel
            {
                TagId = t.BlogTagId,
                Name = t.Name,
                Slug = t.Slug,
                Description = t.Description
            }).ToList();

            var selectedTagIds = new List<int>(article.Tags.Select(t => t.BlogTagId));

            var articleCreateViewModel = new ArticleCreateEditViewModel
            {
                Method = ArticleCreateEditMethodViewModel.Create,
                ArticleId = article.BlogPostId,
                Title = article.Title,
                Slug = article.Slug,
                Description = article.Description,
                MetaDescription = article.MetaDescription,
                SemanticHTML = article.SemanticHTML,
                QuillDelta = article.QuillDelta,
                IsPublished = article.IsPublished ? "on" : "", // Convert IsPublished bool to string for checkbox
                Media = article.Media.Select(m => new ArticleMediaViewModel
                {
                    MediaId = m.MediaId,
                    FilePath = m.FilePath,
                    IsCoverImage = m.IsCoverImage
                }).ToList(),
                AvailableTags = availableTags,
                SelectedTagIds = selectedTagIds
            };

            return View("~/Views/Admin/Articles/CreateAndEdit.cshtml", articleCreateViewModel);
        }

        // POST: Admin/Articles/Create
        [HttpPost("Articles/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleCreateEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var article = await _context.BlogPosts
                    .Include(p => p.Tags)
                    .FirstOrDefaultAsync(p => p.BlogPostId == model.ArticleId);
                if (article == null)
                {
                    return NotFound();
                }

                // Update article with new data
                article.Title = model.Title;
                article.Slug = model.Slug;
                article.Description = model.Description;
                article.MetaDescription = model.MetaDescription;
                article.SemanticHTML = model.SemanticHTML;
                article.QuillDelta = model.QuillDelta;
                article.EditedDate = DateTime.UtcNow;
                article.IsPublished = model.IsPublished == "on"; // Convert ArticleCreateEditViewModel.IsPublished to bool
                article.IsDraft = false; // It's now a posted article, not "draft"

                // Update tags
                if (model.SelectedTagIds != null)
                {
                    article.Tags.Clear(); // Remove existing tags
                    var selectedTags = await _context.BlogTags
                                        .Where(t => model.SelectedTagIds.Contains(t.BlogTagId))
                                        .ToListAsync(); // Fetch the tags to be added
                    foreach (var tag in selectedTags)
                    {
                        article.Tags.Add(tag); // Add new selected tags
                    }
                }
                else
                {
                    article.Tags.Clear(); // Remove all tags
                }

                try
                {
                    _context.Update(article);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Check if article exists
                    if (!_context.BlogPosts.Any(e => e.BlogPostId == model.ArticleId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Redirect to /Admin/Articles after successful update
                return Redirect("/Admin/Articles");
            }

            return View("~/Views/Admin/Articles/CreateAndEdit.cshtml", model);
        }


        // GET: Admin/Articles/Edit/{ArticleId}
        [HttpGet("Articles/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.BlogPosts
                .Include(p => p.Media) // Include media files
                .Include(p => p.Tags) // Include tags
                .FirstOrDefaultAsync(p => p.BlogPostId == id);

            if (article == null)
            {
                return NotFound();
            }

            // Fetch all tags and prepare the view models for available and selected tags
            var allTags = await _context.BlogTags.ToListAsync();
            var availableTags = allTags.Select(t => new ArticleTagViewModel
            {
                TagId = t.BlogTagId,
                Name = t.Name,
                Slug = t.Slug,
                Description = t.Description
            }).ToList();

            var selectedTagIds = new List<int>(article.Tags.Select(t => t.BlogTagId));

            var viewModel = new ArticleCreateEditViewModel
            {
                Method = ArticleCreateEditMethodViewModel.Edit,
                ArticleId = article.BlogPostId,
                Title = article.Title,
                Slug = article.Slug,
                Description = article.Description,
                MetaDescription = article.MetaDescription,
                SemanticHTML = article.SemanticHTML,
                QuillDelta = article.QuillDelta,
                IsPublished = article.IsPublished ? "on" : "", // Convert IsPublished bool to string for checkbox
                Media = article.Media.Select(m => new ArticleMediaViewModel
                {
                    MediaId = m.MediaId,
                    FilePath = m.FilePath,
                    IsCoverImage = m.IsCoverImage
                }).ToList(),
                AvailableTags = availableTags,
                SelectedTagIds = selectedTagIds
            };

            return View("~/Views/Admin/Articles/CreateAndEdit.cshtml", viewModel);
        }


        // POST: Admin/Articles/Edit/{ArticleId}
        [HttpPost("Articles/Edit/{ArticleId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int articleId, ArticleCreateEditViewModel model)
        {
            if (articleId != model.ArticleId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var article = await _context.BlogPosts
                    .Include(p => p.Tags)
                    .FirstOrDefaultAsync(p => p.BlogPostId == articleId);

                if (article == null)
                {
                    return NotFound();
                }

                // If slug has changed, save the old one to history
                if (article.Slug != model.Slug)
                {
                    var slugHistory = new BlogPostSlugHistory
                    {
                        OldSlug = article.Slug,
                        BlogPostId = article.BlogPostId,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.BlogPostSlugHistory.Add(slugHistory);
                }

                // Update article with new data
                article.Title = model.Title;
                article.Slug = model.Slug;
                article.Description = model.Description;
                article.MetaDescription = model.MetaDescription;
                article.SemanticHTML = model.SemanticHTML;
                article.QuillDelta = model.QuillDelta;
                article.EditedDate = DateTime.UtcNow;
                article.IsPublished = model.IsPublished == "on"; // Convert ArticleCreateEditViewModel.IsPublished to bool

                // Update tags
                if (model.SelectedTagIds != null)
                {
                    article.Tags.Clear(); // Remove existing tags
                    var selectedTags = await _context.BlogTags
                                        .Where(t => model.SelectedTagIds.Contains(t.BlogTagId))
                                        .ToListAsync(); // Fetch the tags to be added
                    foreach (var tag in selectedTags)
                    {
                        article.Tags.Add(tag); // Add new selected tags
                    }
                }
                else
                {
                    article.Tags.Clear(); // Remove all tags
                }

                try
                {
                    _context.Update(article);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Check if article exists
                    if (!_context.BlogPosts.Any(e => e.BlogPostId == articleId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Redirect to /Admin/Articles after successful update
                return Redirect("/Admin/Articles");
            }

            return View("~/Views/Admin/Articles/CreateAndEdit.cshtml", model);
        }

        // GET: Admin/Articles/Delete/{ArticleId}
        [HttpGet("Articles/Delete/{articleId:int}")]
        public async Task<IActionResult> Delete(int? articleId)
        {
            if (articleId == null)
            {
                return NotFound();
            }

            var article = await _context.BlogPosts
                .Include(b => b.Media)
                .FirstOrDefaultAsync(m => m.BlogPostId == articleId);
            if (article == null)
            {
                return NotFound();
            }

            // Note: If using external media storage (like Cloudflare Images),
            // you may want to implement cleanup of media files here

            // Deletion from database
            _context.BlogPosts.Remove(article);
            await _context.SaveChangesAsync();

            return Redirect("/Admin/Articles");
        }

        // GET: /Admin/Logout
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Public");
        }

        // GET: /Admin/Tags
        [HttpGet("Tags")]
        public async Task<IActionResult> Tags()
        {
            var tags = await _context.BlogTags
                .OrderByDescending(t => t.EditedDate)
                .Select(t => new ArticleTagViewModel
                {
                    TagId = t.BlogTagId,
                    Name = t.Name,
                    Description = t.Description,
                    Slug = t.Slug
                })
                .ToListAsync();

            return View("~/Views/Admin/Tags/Index.cshtml", tags);
        }

        // GET: Admin/Tags/Create
        [HttpGet("Tags/Create")]
        public IActionResult CreateTag()
        {
            var articleTagCreateViewModel = new ArticleTagCreateEditViewModel
            {
                Method = ArticleTagCreateEditMethodViewModel.Create,
                Name = "",
                Slug = "",
                Description = ""
            };

            return View("~/Views/Admin/Tags/CreateAndEdit.cshtml", articleTagCreateViewModel);
        }

        // POST: Admin/Tags/Create
        [HttpPost("Tags/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTag(ArticleTagCreateEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Challenge();
                }

                var DateNow = DateTime.UtcNow;
                var tag = new BlogTag
                {
                    Name = model.Name,
                    Slug = model.Slug,
                    Description = model.Description,
                    CreatedDate = DateNow,
                    EditedDate = DateNow
                };

                _context.BlogTags.Add(tag);
                await _context.SaveChangesAsync();

                return RedirectToAction("Tags", "Admin");
            }

            return View("~/Views/Admin/Tags/CreateAndEdit.cshtml", model);
        }

        // GET: Admin/Tags/Edit/{TagId}
        [HttpGet("Tags/Edit/{id:int}")]
        public async Task<IActionResult> EditTag(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.BlogTags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            var viewModel = new ArticleTagCreateEditViewModel
            {
                Method = ArticleTagCreateEditMethodViewModel.Edit,
                TagId = tag.BlogTagId,
                Name = tag.Name,
                Slug = tag.Slug,
                Description = tag.Description
            };

            return View("~/Views/Admin/Tags/CreateAndEdit.cshtml", viewModel);
        }

        // POST: Admin/Tags/Edit/{TagId}
        [HttpPost("Tags/Edit/{TagId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTag(int tagId, ArticleTagCreateEditViewModel articleTagCreateEditViewModel)
        {
            if (tagId != articleTagCreateEditViewModel.TagId) // From hidden input in form
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var tag = await _context.BlogTags.FindAsync(tagId);
                if (tag == null)
                {
                    return NotFound();
                }

                // Update tag with new data
                tag.Name = articleTagCreateEditViewModel.Name;
                tag.Slug = articleTagCreateEditViewModel.Slug;
                tag.Description = articleTagCreateEditViewModel.Description;
                tag.EditedDate = DateTime.UtcNow;

                try
                {
                    _context.Update(tag);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Check if tag exists
                    if (!_context.BlogTags.Any(e => e.BlogTagId == tagId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Redirect to /Admin/Tags after successful update
                return Redirect("/Admin/Tags");
            }

            return View("~/Views/Admin/Tags/Edit.cshtml", articleTagCreateEditViewModel);
        }

        // GET: Admin/Tags/Delete/{TagId}
        [HttpGet("Tags/Delete/{tagId:int}")]
        public async Task<IActionResult> DeleteTag(int? tagId)
        {
            if (tagId == null)
            {
                return NotFound();
            }

            var tag = await _context.BlogTags
                .Include(t => t.BlogPosts)
                .FirstOrDefaultAsync(t => t.BlogTagId == tagId);

            if (tag == null)
            {
                return NotFound();
            }

            // Check if the tag is currently used in any articles
            if (tag.BlogPosts.Count != 0)
            {
                return RedirectToAction("Tags", "Admin");
            }

            // Deletion from database
            _context.BlogTags.Remove(tag);
            await _context.SaveChangesAsync();

            return Redirect("/Admin/Tags");
        }

    }
}

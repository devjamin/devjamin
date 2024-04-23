/*
    AdminController.cs, manages admin panel functionality.

    Admin panel access:
    - GET: /Admin (anonymous)
    - GET & POST: /Admin/Login (anonymous)
    - GET: /Admin/Logout

    Blogs management:
    - GET: /Admin/Blogs
    - GET & POST: /Admin/Blogs/Create
    - GET & POST: /Admin/Blogs/Edit/[BlogPostId]
    - GET: /Admin/Blogs/Delete/[BlogPostId]
*/
using Amazon.S3;
using Amazon.S3.Model;
using devjamin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace devjamin.Controllers
{
    [Route("Admin")]
    [Authorize]
    public class AdminController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, BlogDbContext context, IAmazonS3 s3Client, IConfiguration configuration) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly BlogDbContext _context = context;
        private readonly IAmazonS3 _s3Client = s3Client;
        private readonly IConfiguration _configuration = configuration;

        // GET: /Admin
        [HttpGet("")]
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Blogs", "Admin");
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
                    return RedirectToAction("Blogs", "Admin");
                }

                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View("~/Views/Admin/Login.cshtml", adminLoginViewModel);
                }
            }

            return View("~/Views/Admin/Login.cshtml", adminLoginViewModel);
        }

        // GET: Admin/Blogs
        [HttpGet("Blogs")]
        public async Task<IActionResult> GetBlogs()
        {
            var publishedBlogs = await _context.BlogPosts
                .Where(b => b.IsPublished && !b.IsDraft)
                .OrderByDescending(b => b.EditedDate)
                .Select(b => new BlogPostCardViewModel
                {
                    BlogPostId = b.BlogPostId,
                    Title = b.Title,
                    Slug = b.Slug,
                    Description = b.Description,
                    Tags = b.Tags.Select(t => new BlogTagViewModel
                    {
                        Name = t.Name
                    }).ToList()
                })
                .ToListAsync();

            var nonPublishedBlogs = await _context.BlogPosts
                .Where(b => !b.IsPublished && !b.IsDraft)
                .OrderByDescending(b => b.EditedDate)
                .Select(b => new BlogPostCardViewModel
                {
                    BlogPostId = b.BlogPostId,
                    Title = b.Title,
                    Slug = b.Slug,
                    Description = b.Description,
                    Tags = b.Tags.Select(t => new BlogTagViewModel
                    {
                        Name = t.Name
                    }).ToList()
                })
                .ToListAsync();

            var blogsViewModel = new AllBlogsCardViewModel
            {
                PublishedBlogs = publishedBlogs,
                NonPublishedBlogs = nonPublishedBlogs
            };

            return View("~/Views/Admin/Blogs/Index.cshtml", blogsViewModel);
        }


        // GET: Admin/Blogs/Create
        // Create a new empty blog post in the background, then redirect to the create page
        // Upon form submission, the blog post will be modified
        [HttpGet("Blogs/Create")]
        public async Task<IActionResult> Create()
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // create new empty blog
            var DateNow = DateTime.UtcNow;
            var blogPost = new BlogPost
            {
                Title = "",
                Slug = "",
                Description = "",
                Content = "",
                AuthorId = currentUser.Id,
                CreatedDate = DateNow,
                EditedDate = DateNow,
                IsPublished = false
            };

            _context.BlogPosts.Add(blogPost);
            await _context.SaveChangesAsync();

            if (blogPost == null)
            {
                return NotFound();
            }

            // Fetch all tags and prepare the view models for available and selected tags
            var allTags = await _context.BlogTags.ToListAsync();
            var availableTags = allTags.Select(t => new BlogTagViewModel
            {
                BlogTagId = t.BlogTagId,
                Name = t.Name,
                Slug = t.Slug,
                Description = t.Description
            }).ToList();

            var selectedTagIds = new List<int>(blogPost.Tags.Select(t => t.BlogTagId));

            var blogPostCreateViewModel = new BlogPostCreateEditViewModel
            {
                Method = BlogPostCreateEditMethodViewModel.Create,
                BlogPostId = blogPost.BlogPostId,
                Title = blogPost.Title,
                Slug = blogPost.Slug,
                Description = blogPost.Description,
                Content = blogPost.Content,
                IsPublished = blogPost.IsPublished ? "on" : "", // Convert IsPublished bool to string for checkbox
                Media = blogPost.Media.Select(m => new BlogMediaViewModel
                {
                    MediaId = m.MediaId,
                    FilePath = m.FilePath,
                    IsCoverImage = m.IsCoverImage
                }).ToList(),
                AvailableTags = availableTags,
                SelectedTagIds = selectedTagIds
            };

            return View("~/Views/Admin/Blogs/CreateAndEdit.cshtml", blogPostCreateViewModel);
        }

        // POST: Admin/Blogs/Create
        [HttpPost("Blogs/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostCreateEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var blogPost = await _context.BlogPosts
                    .Include(p => p.Tags)
                    .FirstOrDefaultAsync(p => p.BlogPostId == model.BlogPostId);
                if (blogPost == null)
                {
                    return NotFound();
                }

                // Update blog post with new data
                blogPost.Title = model.Title;
                blogPost.Slug = model.Slug;
                blogPost.Description = model.Description;
                blogPost.Content = model.Content;
                blogPost.EditedDate = DateTime.UtcNow;
                blogPost.IsPublished = model.IsPublished == "on"; // Convert BlogPostCreateEditViewModel.IsPublished to bool
                blogPost.IsDraft = false; // It's now a posted blog, not "draft"

                // Update tags
                if (model.SelectedTagIds != null)
                {
                    blogPost.Tags.Clear(); // Remove existing tags
                    var selectedTags = await _context.BlogTags
                                        .Where(t => model.SelectedTagIds.Contains(t.BlogTagId))
                                        .ToListAsync(); // Fetch the tags to be added
                    foreach (var tag in selectedTags)
                    {
                        blogPost.Tags.Add(tag); // Add new selected tags
                    }
                }
                else
                {
                    blogPost.Tags.Clear(); // Remove all tags
                }

                try
                {
                    _context.Update(blogPost);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Check if blog post exists
                    if (!_context.BlogPosts.Any(e => e.BlogPostId == model.BlogPostId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Redirect to /Admin/Blogs after successful update
                return Redirect("/Admin/Blogs");
            }

            return View("~/Views/Admin/Blogs/CreateAndEdit.cshtml", model);
        }


        // GET: Admin/Blogs/Edit/{BlogPostId}
        [HttpGet("Blogs/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blogPost = await _context.BlogPosts
                .Include(p => p.Media) // Include media files
                .Include(p => p.Tags) // Include tags
                .FirstOrDefaultAsync(p => p.BlogPostId == id);

            if (blogPost == null)
            {
                return NotFound();
            }

            // Fetch all tags and prepare the view models for available and selected tags
            var allTags = await _context.BlogTags.ToListAsync();
            var availableTags = allTags.Select(t => new BlogTagViewModel
            {
                BlogTagId = t.BlogTagId,
                Name = t.Name,
                Slug = t.Slug,
                Description = t.Description
            }).ToList();

            var selectedTagIds = new List<int>(blogPost.Tags.Select(t => t.BlogTagId));


            var viewModel = new BlogPostCreateEditViewModel
            {
                Method = BlogPostCreateEditMethodViewModel.Edit,
                BlogPostId = blogPost.BlogPostId,
                Title = blogPost.Title,
                Slug = blogPost.Slug,
                Description = blogPost.Description,
                Content = blogPost.Content,
                IsPublished = blogPost.IsPublished ? "on" : "", // Convert IsPublished bool to string for checkbox
                Media = blogPost.Media.Select(m => new BlogMediaViewModel
                {
                    MediaId = m.MediaId,
                    FilePath = m.FilePath,
                    IsCoverImage = m.IsCoverImage
                }).ToList(),
                AvailableTags = availableTags,
                SelectedTagIds = selectedTagIds
            };

            return View("~/Views/Admin/Blogs/CreateAndEdit.cshtml", viewModel);
        }


        // POST: Admin/Blogs/Edit/{BlogPostId}
        [HttpPost("Blogs/Edit/{BlogPostId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int blogPostId, BlogPostCreateEditViewModel BlogPostCreateEditViewModel)
        {
            if (blogPostId != BlogPostCreateEditViewModel.BlogPostId) // From hidden input in form
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var blogPost = await _context.BlogPosts
                    .Include(p => p.Tags)
                    .FirstOrDefaultAsync(p => p.BlogPostId == blogPostId);
                if (blogPost == null)
                {
                    return NotFound();
                }

                // Update blog post with new data
                blogPost.Title = BlogPostCreateEditViewModel.Title;
                blogPost.Slug = BlogPostCreateEditViewModel.Slug;
                blogPost.Description = BlogPostCreateEditViewModel.Description;
                blogPost.Content = BlogPostCreateEditViewModel.Content;
                blogPost.EditedDate = DateTime.UtcNow;
                blogPost.IsPublished = BlogPostCreateEditViewModel.IsPublished == "on"; // Convert BlogPostCreateEditViewModel.IsPublished to bool

                // Update tags
                if (BlogPostCreateEditViewModel.SelectedTagIds != null)
                {
                    blogPost.Tags.Clear(); // Remove existing tags
                    var selectedTags = await _context.BlogTags
                                        .Where(t => BlogPostCreateEditViewModel.SelectedTagIds.Contains(t.BlogTagId))
                                        .ToListAsync(); // Fetch the tags to be added
                    foreach (var tag in selectedTags)
                    {
                        blogPost.Tags.Add(tag); // Add new selected tags
                    }
                }
                else
                {
                    blogPost.Tags.Clear(); // Remove all tags
                }

                try
                {
                    _context.Update(blogPost);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Check if blog post exists
                    if (!_context.BlogPosts.Any(e => e.BlogPostId == blogPostId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Redirect to /Admin/Blogs after successful update
                return Redirect("/Admin/Blogs");
            }

            return View("~/Views/Admin/Blogs/Edit.cshtml", BlogPostCreateEditViewModel);
        }

        // GET: Admin/Blogs/Delete/{BlogPostId}
        [HttpGet("Blogs/Delete/{blogPostId:int}")]
        public async Task<IActionResult> Delete(int? blogPostId)
        {
            if (blogPostId == null)
            {
                return NotFound();
            }

            var blogPost = await _context.BlogPosts
                .Include(b => b.Media)
                .FirstOrDefaultAsync(m => m.BlogPostId == blogPostId);
            if (blogPost == null)
            {
                return NotFound();
            }

            // Delete associated images from S3 bucket
            foreach (var mediaItem in blogPost.Media.ToList())
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = _configuration.GetValue<string>("EnvironmentVariables:BUCKETNAME"),
                    Key = mediaItem.FilePath
                };

                // Should notify user if deletion fails, but it is not critical
                try
                {
                    var response = await _s3Client.DeleteObjectAsync(deleteObjectRequest);
                }
                catch (Exception)
                {
                    // log exception 
                }
            }

            // Deletion from database
            _context.BlogPosts.Remove(blogPost);
            await _context.SaveChangesAsync();

            return Redirect("/Admin/Blogs");
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
                .Select(t => new BlogTagViewModel
                {
                    BlogTagId = t.BlogTagId,
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
            var blogTagCreateViewModel = new BlogTagCreateEditViewModel
            {
                Method = BlogTagCreateEditMethodViewModel.Create,
                Name = "",
                Slug = "",
                Description = ""
            };

            return View("~/Views/Admin/Tags/CreateAndEdit.cshtml", blogTagCreateViewModel);
        }

        // POST: Admin/Tags/Create
        [HttpPost("Tags/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTag(BlogTagCreateEditViewModel model)
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
                var blogTag = new BlogTag
                {
                    Name = model.Name,
                    Slug = model.Slug,
                    Description = model.Description,
                    CreatedDate = DateNow,
                    EditedDate = DateNow
                };

                _context.BlogTags.Add(blogTag);
                await _context.SaveChangesAsync();

                return RedirectToAction("Tags", "Admin");
            }

            return View("~/Views/Admin/Tags/CreateAndEdit.cshtml", model);
        }

        // GET: Admin/Tags/Edit/{BlogTagId}
        [HttpGet("Tags/Edit/{id:int}")]
        public async Task<IActionResult> EditTag(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blogTag = await _context.BlogTags.FindAsync(id);
            if (blogTag == null)
            {
                return NotFound();
            }

            var viewModel = new BlogTagCreateEditViewModel
            {
                Method = BlogTagCreateEditMethodViewModel.Edit,
                BlogTagId = blogTag.BlogTagId,
                Name = blogTag.Name,
                Slug = blogTag.Slug,
                Description = blogTag.Description
            };

            return View("~/Views/Admin/Tags/CreateAndEdit.cshtml", viewModel);
        }

        // POST: Admin/Tags/Edit/{BlogTagId}
        [HttpPost("Tags/Edit/{BlogTagId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTag(int blogTagId, BlogTagCreateEditViewModel BlogTagCreateEditViewModel)
        {
            if (blogTagId != BlogTagCreateEditViewModel.BlogTagId) // From hidden input in form
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var blogTag = await _context.BlogTags.FindAsync(blogTagId);
                if (blogTag == null)
                {
                    return NotFound();
                }

                // Update blog tag with new data
                blogTag.Name = BlogTagCreateEditViewModel.Name;
                blogTag.Slug = BlogTagCreateEditViewModel.Slug;
                blogTag.Description = BlogTagCreateEditViewModel.Description;
                blogTag.EditedDate = DateTime.UtcNow;

                try
                {
                    _context.Update(blogTag);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Check if blog tag exists
                    if (!_context.BlogTags.Any(e => e.BlogTagId == blogTagId))
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

            return View("~/Views/Admin/Tags/Edit.cshtml", BlogTagCreateEditViewModel);
        }

        // GET: Admin/Tags/Delete/{BlogTagId}
        [HttpGet("Tags/Delete/{blogTagId:int}")]
        public async Task<IActionResult> DeleteTag(int? blogTagId)
        {
            if (blogTagId == null)
            {
                return NotFound();
            }

            var blogTag = await _context.BlogTags
                .Include(t => t.BlogPosts)
                .FirstOrDefaultAsync(t => t.BlogTagId == blogTagId);

            if (blogTag == null)
            {
                return NotFound();
            }

            // Check if the tag is currently used in any blog posts
            if (blogTag.BlogPosts.Count != 0)
            {
                return RedirectToAction("Tags", "Admin");
            }

            // Deletion from database
            _context.BlogTags.Remove(blogTag);
            await _context.SaveChangesAsync();

            return Redirect("/Admin/Tags");
        }

    }
}

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
            var activeBlogs = await _context.BlogPosts
                .Where(b => b.IsPublished && !b.IsDraft)
                .OrderByDescending(b => b.EditedDate)
                .Select(b => new BlogPostCardViewModel
                {
                    BlogPostId = b.BlogPostId,
                    Title = b.Title,
                    Slug = b.Slug,
                    Description = b.Description,
                })
                .ToListAsync();

            var nonActiveBlogs = await _context.BlogPosts
                .Where(b => !b.IsPublished && !b.IsDraft)
                .OrderByDescending(b => b.EditedDate)
                .Select(b => new BlogPostCardViewModel
                {
                    BlogPostId = b.BlogPostId,
                    Title = b.Title,
                    Slug = b.Slug,
                    Description = b.Description,
                })
                .ToListAsync();

            var blogsViewModel = new AllBlogsCardViewModel
            {
                ActiveBlogs = activeBlogs,
                NonActiveBlogs = nonActiveBlogs
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
                PublishedDate = DateNow,
                EditedDate = DateNow,
                IsPublished = false
            };

            _context.BlogPosts.Add(blogPost);
            await _context.SaveChangesAsync();

            var blogPostCreateViewModel = new BlogPostCreateEditViewModel
            {
                Method = BlogPostCreateEditMethodViewModel.Create,
                BlogPostId = blogPost.BlogPostId,
                Title = blogPost.Title,
                Slug = blogPost.Slug,
                Description = blogPost.Description,
                Content = blogPost.Content,
                IsPublished = blogPost.IsPublished ? "on" : ""
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
                // Get the existing blog post using the ID
                var existingBlogPost = await _context.BlogPosts.FindAsync(model.BlogPostId);
                if (existingBlogPost == null)
                {
                    return NotFound();
                }

                // Update the existing blog post properties
                existingBlogPost.IsDraft = false; // No longer a draft
                existingBlogPost.Title = model.Title;
                existingBlogPost.Slug = model.Slug;
                existingBlogPost.Description = model.Description;
                existingBlogPost.Content = model.Content;
                existingBlogPost.IsPublished = model.IsPublished == "on";
                existingBlogPost.EditedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return RedirectToAction("Blogs", "Admin");
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
                .FirstOrDefaultAsync(p => p.BlogPostId == id);

            if (blogPost == null)
            {
                return NotFound();
            }

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
                }).ToList()
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
                var blogPost = await _context.BlogPosts.FindAsync(blogPostId);
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
    }
}

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
using devjamin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace devjamin.Controllers
{
    [Route("Admin")]
    [Authorize]
    public class AdminController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, BlogDbContext context) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly BlogDbContext _context = context;

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
        [Authorize]
        public async Task<IActionResult> GetBlogs()
        {
            var allBlogs = await _context.BlogPosts.ToListAsync();
            var blogsViewModel = new BlogsViewModel
            {
                ActiveBlogs = allBlogs.Where(b => b.IsPublished).ToList(),
                NonActiveBlogs = allBlogs.Where(b => !b.IsPublished).ToList()
            };

            return View("~/Views/Admin/Blogs/Index.cshtml", blogsViewModel);
        }


        // GET: Admin/Blogs/Create
        [HttpGet("Blogs/Create")]
        [Authorize]
        public IActionResult Create()
        {
            var blogPostViewModel = new BlogPostViewModel();
            return View("~/Views/Admin/Blogs/Create.cshtml", blogPostViewModel);
        }

        // POST: Admin/Blogs/Create
        [HttpPost("Blogs/Create")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostViewModel blogPostViewModel)
        {
            if (ModelState.IsValid)
            {
                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Challenge();
                }

                var blogPost = new BlogPost
                {
                    Title = blogPostViewModel.Title,
                    Slug = blogPostViewModel.Slug,
                    Description = blogPostViewModel.Description,
                    Content = blogPostViewModel.Content,
                    AuthorId = currentUser.Id,
                    PublishedDate = DateTime.UtcNow,
                    IsPublished = blogPostViewModel.IsPublished == "on" // BlogPostViewModel.IsPublished is a checkbox string (on/null), convert to bool
                };

                _context.BlogPosts.Add(blogPost);
                await _context.SaveChangesAsync();

                return RedirectToAction("Blogs", "Admin"); // Redirect to /Admin/Blogs
            }

            return View("~/Views/Admin/Blogs/Create.cshtml", blogPostViewModel);
        }

        // GET: Admin/Blogs/Edit/{BlogPostId}
        [HttpGet("Blogs/Edit/{id:int}")]
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null)
            {
                return NotFound();
            }

            var viewModel = new BlogPostViewModel
            {
                BlogPostId = blogPost.BlogPostId,
                Title = blogPost.Title,
                Slug = blogPost.Slug,
                Description = blogPost.Description,
                Content = blogPost.Content,
                IsPublished = blogPost.IsPublished ? "on" : "" // Convert IsPublished bool to string for checkbox
            };

            return View("~/Views/Admin/Blogs/Edit.cshtml", viewModel);
        }

        // POST: Admin/Blogs/Edit/{BlogPostId}
        [HttpPost("Blogs/Edit/{BlogPostId:int}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int blogPostId, BlogPostViewModel blogPostViewModel)
        {
            if (blogPostId != blogPostViewModel.BlogPostId) // From hidden input in form
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
                blogPost.Title = blogPostViewModel.Title;
                blogPost.Slug = blogPostViewModel.Slug;
                blogPost.Description = blogPostViewModel.Description;
                blogPost.Content = blogPostViewModel.Content;
                blogPost.IsPublished = blogPostViewModel.IsPublished == "on"; // Convert BlogPostViewModel.IsPublished to bool

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

            return View("~/Views/Admin/Blogs/Edit.cshtml", blogPostViewModel);
        }

        // GET: Admin/Blogs/Delete/[ID]
        [HttpGet("Blogs/Delete/{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blogPost = await _context.BlogPosts
                .FirstOrDefaultAsync(m => m.BlogPostId == id);
            if (blogPost == null)
            {
                return NotFound();
            }

            _context.BlogPosts.Remove(blogPost);
            await _context.SaveChangesAsync();

            return Redirect("/Admin/Blogs");
        }

        // GET: /Admin/Logout
        [HttpGet("Logout")]
        [Authorize] // Only allow logged in users to log out
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}

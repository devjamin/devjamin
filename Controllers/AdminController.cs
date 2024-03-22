using BlogSite.Models;
using devjamin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace devjamin.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly BlogDbContext _context;

        // dependency injection
        public AdminController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AdminController> logger, BlogDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // Get: /Admin
        // If not signed in redirect to /Admin/Login otherwise redirect to /Admin/Blogs
        [HttpGet]
        public IActionResult Index()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Blogs");
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        // GET: Admin/Login
        [HttpGet]
        public IActionResult Login()
        {
            var model = new AdminLoginViewModel()
            {
                Username = "",
                Password = ""
            };
            return View(model);
        }

        // POST: /Admin/Login
        [HttpPost]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, true, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    //_logger.LogInformation($"ReturnUrl: {returnUrl}");
                    return RedirectToAction("Index", "Admin");
                }

                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // something failed, redisplay form
            return View(model);
        }

        // GET: Admin/Blogs
        [Authorize]
        [HttpGet("/Admin/Blogs")]
        public async Task<IActionResult> GetBlogs()
        {
            var allBlogs = await _context.BlogPosts.ToListAsync();
            var viewModel = new BlogsViewModel
            {
                ActiveBlogs = allBlogs.Where(b => b.IsPublished).ToList(),
                NonActiveBlogs = allBlogs.Where(b => !b.IsPublished).ToList()
            };

            return View("~/Views/Admin/Blogs/Index.cshtml", viewModel);
        }


        // GET: Admin/Blogs/Create
        [Authorize]
        [HttpGet("/Admin/Blogs/Create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/Blogs/Create.cshtml", new BlogPostViewModel());
        }

        // POST: Admin/Blogs/Create
        [Authorize]
        [HttpPost("/Admin/Blogs/Create")]
        public async Task<IActionResult> Create(BlogPostViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge(); // Ensure user is logged in

                var blogPost = new BlogPost
                {
                    Title = model.Title,
                    Slug = model.Slug,
                    Description = model.Description,
                    Content = model.Content,
                    AuthorId = currentUser.Id,
                    PublishedDate = DateTime.UtcNow,
                    IsPublished = model.IsPublished == "on" ? true : false // Set IsPublished based on the form input
                };

                _context.BlogPosts.Add(blogPost);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index"); // Redirect to a suitable page after creation
            }

            // Model state is invalid, show the form again with validation errors
            return View("~/Views/Admin/Blogs/Create.cshtml", model);
        }

        // GET: Admin/Blogs/Edit/[BlogPostId]
        [Authorize]
        [HttpGet("/Admin/Blogs/Edit/{id:int}")]
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
                IsPublished = blogPost.IsPublished ? "on" : "" // Set IsPublished based on the form input
            };

            return View("~/Views/Admin/Blogs/Edit.cshtml", viewModel);
        }

        // POST: Admin/Blogs/Edit/5
        [HttpPost("/Admin/Blogs/Edit/{id:int}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPostViewModel viewModel)
        {
            if (id != viewModel.BlogPostId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var blogPost = await _context.BlogPosts.FindAsync(id);
                if (blogPost == null)
                {
                    return NotFound();
                }

                blogPost.Title = viewModel.Title;
                blogPost.Slug = viewModel.Slug;
                blogPost.Description = viewModel.Description;
                blogPost.Content = viewModel.Content;
                blogPost.IsPublished = viewModel.IsPublished == "on" ? true : false; // Set IsPublished based on the form input

                try
                {
                    _context.Update(blogPost);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogPostExists(blogPost.BlogPostId))
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
            return View("~/Views/Admin/Blogs/Edit.cshtml", viewModel);
        }

        private bool BlogPostExists(int id)
        {
            return _context.BlogPosts.Any(e => e.BlogPostId == id);
        }

        // GET: Admin/Blogs/Delete/[ID]
        [Authorize]
        [HttpGet("/Admin/Blogs/Delete/{id:int}")]
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
        [HttpGet]
        [Authorize] // Only allow logged in users to log out
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // Method to create an admin user. !!! DELETE AFTER USE, NOT FOR PRODUCTION !!!
        [HttpPost]
        [Route("Admin/CreateAdminUser")]
        public async Task<IActionResult> CreateAdminUser([FromBody] AdminUserViewModel model)
        {
            if (model == null)
            {
                return BadRequest("Invalid user data.");
            }

            var adminExists = await _userManager.FindByNameAsync(model.UserName);
            if (adminExists == null)
            {
                var adminUser = new ApplicationUser
                {
                    FullName = model.FullName,
                    UserName = model.UserName,
                    Email = model.Email // Ensure this is a valid email format
                };
                var createAdminResult = await _userManager.CreateAsync(adminUser, model.Password);
                if (createAdminResult.Succeeded)
                {
                    return Ok("Admin user created successfully, now go close this endpoint.");
                }
                else
                {
                    var errors = string.Join("; ", createAdminResult.Errors.Select(e => e.Description));
                    return Ok($"Admin user creation failed: {errors}");
                }
            }
            else
            {
                return Ok("Admin user already exists.");
            }
        }


    }
}

/*
    ! REMOVE IN PRODUCTION !
    SystemInitController.cs, creates initial admin user.

    POST: /SystemInit/CreateAdminUser
    {
        "FullName": "",
        "UserName": "",
        "Email": ""
    }
*/
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using devjamin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace devjamin.Controllers
{
    public class SystemInitController : IHostedService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public SystemInitController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Read admin details from configuration or environment variables
            var adminEmail = _configuration["AdminUser:Email"];
            var adminUsername = _configuration["AdminUser:UserName"];
            var adminPassword = _configuration["AdminUser:Password"];
            var adminFullName = _configuration["AdminUser:FullName"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                Console.WriteLine("Admin user configuration not found. Skipping admin user creation.");
                return;
            }

            var userExists = await _userManager.FindByEmailAsync(adminEmail);
            if (userExists == null)
            {
                ApplicationUser user = new()
                {
                    Email = adminEmail,
                    UserName = adminUsername,
                    FullName = adminFullName,
                    ProfilePicturePath = "/images/default-profile-picture.png"
                };

                var result = await _userManager.CreateAsync(user, adminPassword);
                if (result.Succeeded)
                {
                    Console.WriteLine("Admin user created successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to create admin user:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"- {error.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Admin user already exists.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}


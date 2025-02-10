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
using Microsoft.Extensions.DependencyInjection;

namespace devjamin.Controllers
{
    public class SystemInitController : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public SystemInitController(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Create a new scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Read admin details from configuration
            var adminEmail = _configuration["AdminUser:Email"];
            var adminUsername = _configuration["AdminUser:UserName"];
            var adminPassword = _configuration["AdminUser:Password"];
            var adminFullName = _configuration["AdminUser:FullName"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                Console.WriteLine("Admin user configuration not found. Skipping admin user creation.");
                return;
            }

            var userExists = await userManager.FindByEmailAsync(adminEmail);
            if (userExists == null)
            {
                ApplicationUser user = new()
                {
                    Email = adminEmail,
                    UserName = adminUsername,
                    FullName = adminFullName,
                    ProfilePicturePath = "/images/default-profile-picture.png"
                };

                var result = await userManager.CreateAsync(user, adminPassword);
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


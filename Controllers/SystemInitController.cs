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

namespace devjamin.Controllers
{
    [Route("SystemInit")]
    public class SystemInitController(UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        [HttpPost]
        [Route("CreateAdminUser")]
        public async Task<IActionResult> CreateAdminUser([FromBody] AdminUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userExists = await _userManager.FindByEmailAsync(model.Email);
                if (userExists == null)
                {
                    ApplicationUser user = new()
                    {
                        Email = model.Email,
                        UserName = model.UserName,
                        FullName = model.FullName
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        return Ok("Admin user created successfully.");
                    }
                    else
                    {
                        return BadRequest(result.Errors);
                    }
                }
                return BadRequest("User already exists.");
            }
            return BadRequest("Invalid payload.");
        }
    }
}

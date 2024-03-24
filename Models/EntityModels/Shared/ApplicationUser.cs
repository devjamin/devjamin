/*
    ApplicationUser.cs, extends IdentityUser to include a FullName property.
*/
using Microsoft.AspNetCore.Identity;

namespace devjamin.Models
{
    public class ApplicationUser : IdentityUser
    {
        public required string FullName { get; set; }
    }
}

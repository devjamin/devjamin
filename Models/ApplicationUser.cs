using Microsoft.AspNetCore.Identity;

// Extending the ApplicationUser class to include a FullName property
namespace devjamin.Models
{
  public class ApplicationUser : IdentityUser
  {
    public required string FullName { get; set; }
  }
}

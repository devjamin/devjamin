using Microsoft.AspNetCore.Identity;

// Extending the ApplicationUser class to include a FullName property
namespace BlogSite.Models
{
  public class ApplicationUser : IdentityUser
  {
    public required string FullName { get; set; }
  }
}

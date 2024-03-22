using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
  public class AdminLoginViewModel
  {
    [Required(ErrorMessage = "Enter a username.")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Enter a password.")]
    [DataType(DataType.Password)]
    public required string Password { get; set; }
  }
}

/*
    AdminLoginViewModel.cs, view model for admin login.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Enter a username.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}

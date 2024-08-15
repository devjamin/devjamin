/*
    AdminLoginViewModel.cs, view model for admin login.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "You'll never guess this one.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "This is 'admin' backwards.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}

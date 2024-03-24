/*
    CreateAdminUserViewModel.cs, view model for creating an the initial admin user.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    public class CreateAdminUserViewModel
    {
        [Required(ErrorMessage = "Provide your full name.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Provide a username.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Provide an email, can be dummy.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}

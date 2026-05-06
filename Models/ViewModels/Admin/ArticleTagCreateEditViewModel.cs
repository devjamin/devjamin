/*
    ArticleTagCreateEditViewModel.cs, view model for creating and editing article tags.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    // DRY principle, create an enum for the method
    // This is to determine if the view model is for creating or editing tag
    public enum ArticleTagCreateEditMethodViewModel
    {
        Create,
        Edit
    }

    public class ArticleTagCreateEditViewModel
    {
        // method can either be create or edit
        public ArticleTagCreateEditMethodViewModel Method { get; set; }

        public int TagId { get; set; }

        [Required(ErrorMessage = "Please enter a name.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a slug.")]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a description.")]
        public string Description { get; set; } = string.Empty;
    }
}

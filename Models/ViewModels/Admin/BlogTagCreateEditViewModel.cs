/*
    BlogTagCreateEditViewModel.cs, view model for creating and editing blog tags.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    // DRY principle, create an enum for the method
    // This is to determine if the view model is for creating or editing tag
    public enum BlogTagCreateEditMethodViewModel
    {
        Create,
        Edit
    }

    public class BlogTagCreateEditViewModel
    {
        // method can either be create or edit
        public BlogTagCreateEditMethodViewModel Method { get; set; }

        public int BlogTagId { get; set; }

        [Required(ErrorMessage = "Please enter a name.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a slug.")]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a description.")]
        public string Description { get; set; } = string.Empty;
    }
}

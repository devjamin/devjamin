/*
    BlogPostCreateEditViewModel.cs, view model for creating and editing blog posts.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    // DRY principle, create an enum for the method
    // This is to determine if the view model is for creating or editing blog post
    public enum BlogPostCreateEditMethodViewModel
    {
        Create,
        Edit
    }

    public class BlogPostCreateEditViewModel
    {
        // Method can either be create or edit
        public BlogPostCreateEditMethodViewModel Method { get; set; }

        public int BlogPostId { get; set; }

        [Required(ErrorMessage = "Please enter a title.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a slug.")]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a description.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a meta description.")]
        [MaxLength(160, ErrorMessage = "Meta description cannot exceed 160 characters.")]
        public string MetaDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the content.")]
        public string Content { get; set; } = string.Empty;

        public string? QuillContent { get; set; }

        public string? IsPublished { get; set; }

        // Include media files
        public List<BlogMediaViewModel> Media { get; set; } = [];

        public List<BlogTagViewModel> AvailableTags { get; set; } = [];
        public List<int> SelectedTagIds { get; set; } = [];
    }
}

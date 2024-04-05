/*
    BlogPostCreateEditViewModel.cs, view model for creating and editing blog posts.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    public class BlogPostCreateEditViewModel
    {
        public int BlogPostId { get; set; }

        [Required(ErrorMessage = "Please enter a title.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a slug.")]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a description.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the content.")]
        public string Content { get; set; } = string.Empty;

        public string? IsPublished { get; set; }

        // Include media files
        public List<BlogMediaViewModel> Media { get; set; } = [];
    }
}

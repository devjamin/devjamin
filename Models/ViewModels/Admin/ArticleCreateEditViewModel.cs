/*
    ArticleCreateEditViewModel.cs, view model for creating and editing articles.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    // DRY principle, create an enum for the method
    // This is to determine if the view model is for creating or editing article
    public enum ArticleCreateEditMethodViewModel
    {
        Create,
        Edit
    }

    public class ArticleCreateEditViewModel
    {
        // Method can either be create or edit
        public ArticleCreateEditMethodViewModel Method { get; set; }

        public int ArticleId { get; set; }

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
        public string SemanticHTML { get; set; } = string.Empty;

        public string? QuillDelta { get; set; }

        public string? IsPublished { get; set; }

        // Include media files
        public List<ArticleMediaViewModel> Media { get; set; } = [];

        public List<ArticleTagViewModel> AvailableTags { get; set; } = [];
        public List<int> SelectedTagIds { get; set; } = [];
    }
}

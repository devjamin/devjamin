/*
    ArticleCardViewModel.cs, view for article preview card.
*/
namespace devjamin.Models
{
    public class ArticleCardViewModel
    {
        public int ArticleId { get; set; }

        public required string Title { get; set; }

        public required string Slug { get; set; }

        public required string Description { get; set; }

        public string? CoverImagePath { get; set; }

        public List<ArticleTagViewModel> Tags { get; set; } = [];
    }
}

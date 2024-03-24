/*
    BlogPostCardViewModel.cs, view for blog post preview card.
*/
namespace devjamin.Models
{
    public class BlogPostCardViewModel
    {
        public int BlogPostId { get; set; }

        public required string Title { get; set; }

        public required string Slug { get; set; }

        public required string Description { get; set; }
    }
}

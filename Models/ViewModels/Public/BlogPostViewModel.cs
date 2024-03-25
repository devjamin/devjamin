/*
    BlogPostViewModel.cs, public facing view for full blog post.
*/
namespace devjamin.Models
{
    public class BlogPostViewModel
    {
        public int BlogPostId { get; set; }

        public required string Title { get; set; }

        public required string Slug { get; set; }

        public required string Description { get; set; }

        public required DateTime PublishedDate { get; set; } = DateTime.UtcNow;

        public required string Content { get; set; }

        public required string AuthorFullName { get; set; }

        public required string AuthorProfilePicturePath { get; set; }

        public int ReadTimeMinuteEstimate { get; set; }

    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace devjamin.Models
{
    public class BlogMedia
    {
        public int Id { get; set; }

        public required string FilePath { get; set; }

        public required string ContentType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public required int BlogPostId { get; set; }

        [ForeignKey("BlogPostId")]
        public virtual BlogPost? BlogPost { get; set; }
    }
}

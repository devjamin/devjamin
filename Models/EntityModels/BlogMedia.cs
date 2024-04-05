using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devjamin.Models
{
    public class BlogMedia
    {
        [Key]
        public int MediaId { get; set; }

        public required string FilePath { get; set; }

        public required string ContentType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsCoverImage { get; set; } = false;

        // Nullable as we don't have BlogPostId until it's created
        public required int? BlogPostId { get; set; }

        [ForeignKey("BlogPostId")]
        public virtual BlogPost? BlogPost { get; set; }
    }
}

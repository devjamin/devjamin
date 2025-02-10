using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    public class BlogPostSlugHistory
    {
        [Key]
        public int BlogPostSlugHistoryId { get; set; }

        [Required]
        public required string OldSlug { get; set; }

        [Required]
        public required int BlogPostId { get; set; }

        public required DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual BlogPost BlogPost { get; set; } = null!;
    }
}
using System.ComponentModel.DataAnnotations;
using devjamin.Models;

namespace devjamin
{
    public class BlogTag
    {
        [Key]
        public int BlogTagId { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Slug { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Description { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        public DateTime EditedDate { get; set; } = DateTime.UtcNow;

        // Navigation property for related blog posts
        public virtual ICollection<BlogPost> BlogPosts { get; set; } = [];
    }
}
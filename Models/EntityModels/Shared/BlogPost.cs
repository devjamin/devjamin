/*
    BlogPost.cs, EntityModel for blog posts.
*/
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devjamin.Models
{
    public class BlogPost
    {
        [Key]
        public int BlogPostId { get; set; }

        public required string Title { get; set; }

        public required string Slug { get; set; }

        public required string Description { get; set; }

        [DataType(DataType.MultilineText)]
        public required string Content { get; set; }

        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public required DateTime PublishedDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public required DateTime EditedDate { get; set; } = DateTime.UtcNow;

        public required bool IsPublished { get; set; } = false;

        public required string AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public virtual ApplicationUser? Author { get; set; }

        // Navigation property for media files
        public virtual ICollection<BlogMedia> Media { get; set; } = [];
    }
}

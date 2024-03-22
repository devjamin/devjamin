using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogSite.Models
{
  public class BlogPostViewModel
  {
    public int BlogPostId { get; set; }

    [Required(ErrorMessage = "Please enter a title.")]
    public string Title { get; set; }

    public string Slug { get; set; }

    public string Description { get; set; }

    [Required(ErrorMessage = "Please enter the content.")]
    public string Content { get; set; }

    public string? IsPublished { get; set; } // "on" == true, null == false
  }

  public class BlogsViewModel
  {
    public List<BlogPost> ActiveBlogs { get; set; }
    public List<BlogPost> NonActiveBlogs { get; set; }
  }


  public class BlogPost
  {
    [Key]
    public int BlogPostId { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Slug { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    [DataType(DataType.MultilineText)]
    public string Content { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime PublishedDate { get; set; } = DateTime.UtcNow;

    public bool IsPublished { get; set; }

    [Required]
    public string AuthorId { get; set; }

    [ForeignKey("AuthorId")]
    public virtual ApplicationUser Author { get; set; }
  }

  public class BlogDbContext : IdentityDbContext<ApplicationUser>
  {
    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options) { }

    public DbSet<BlogPost> BlogPosts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
      base.OnModelCreating(builder);

      builder.Entity<BlogPost>()
          .HasOne<ApplicationUser>(p => p.Author)
          .WithMany()
          .HasForeignKey(p => p.AuthorId)
          .IsRequired();
    }
  }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace devjamin.Models
{
	public class BlogDbContext(DbContextOptions<BlogDbContext> options) : IdentityDbContext<ApplicationUser>(options)
	{
		public DbSet<BlogPost> BlogPosts { get; set; }
		public DbSet<BlogMedia> BlogMedia { get; set; }
		public DbSet<BlogTag> BlogTags { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Configure one-to-many relationship between BlogPost and BlogMedia
			builder.Entity<BlogPost>()
				.HasOne<ApplicationUser>(p => p.Author)
				.WithMany()
				.HasForeignKey(p => p.AuthorId)
				.IsRequired();

			// Configure many-to-many relationship between BlogPost and BlogTag
			builder.Entity<BlogPost>()
				.HasMany(p => p.Tags)
				.WithMany(t => t.BlogPosts)
				.UsingEntity(j => j.ToTable("BlogPostTags"));
		}
	}
}
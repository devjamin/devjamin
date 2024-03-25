using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace devjamin.Models
{
	public class BlogDbContext(DbContextOptions<BlogDbContext> options) : IdentityDbContext<ApplicationUser>(options)
	{
		public DbSet<BlogPost> BlogPosts { get; set; }
		public DbSet<BlogMedia> BlogMedia { get; set; }

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
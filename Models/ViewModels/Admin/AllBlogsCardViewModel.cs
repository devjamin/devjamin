/*
    AllBlogsCardViewModel.cs, view model for listing all blog posts (published, non-published).
*/
namespace devjamin.Models
{
    public class AllBlogsCardViewModel
    {
        public List<BlogPostCardViewModel> PublishedBlogs { get; set; } = [];
        public List<BlogPostCardViewModel> NonPublishedBlogs { get; set; } = [];
    }
}

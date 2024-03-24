/*
    AllBlogsCardViewModel.cs, view model for listing all blog posts (active and non-active).
*/
namespace devjamin.Models
{
    public class AllBlogsCardViewModel
    {
        public List<BlogPostCardViewModel> ActiveBlogs { get; set; } = [];
        public List<BlogPostCardViewModel> NonActiveBlogs { get; set; } = [];
    }
}

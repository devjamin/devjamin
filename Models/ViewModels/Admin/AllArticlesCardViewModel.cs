/*
    AllArticlesCardViewModel.cs, view model for listing all articles (published, non-published).
*/
namespace devjamin.Models
{
    public class AllArticlesCardViewModel
    {
        public List<ArticleCardViewModel> PublishedArticles { get; set; } = [];
        public List<ArticleCardViewModel> NonPublishedArticles { get; set; } = [];
    }
}

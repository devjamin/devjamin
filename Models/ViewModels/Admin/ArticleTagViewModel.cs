/*
    ArticleTagViewModel.cs, view model viewing article tags.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    public class ArticleTagViewModel
    {
        public int TagId { get; set; }

        [MaxLength(35)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(35)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;
    }
}

/*
    BlogTagViewModel.cs, view model viewing blog tags.
*/
using System.ComponentModel.DataAnnotations;

namespace devjamin.Models
{
    public class BlogTagViewModel
    {
        public int BlogTagId { get; set; }

        [MaxLength(35)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(35)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;
    }
}

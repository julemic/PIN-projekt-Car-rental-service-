using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        [MaxLength(300)]
        public string? ShortDescription { get; set; }

        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string? AuthorId { get; set; }
        public ApplicationUser? Author { get; set; }
    }
}

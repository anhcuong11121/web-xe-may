using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class News
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Published";

    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = "News";

    public DateTime? PublishedAt { get; set; }

    public Guid AuthorId { get; set; }

    public virtual AppUser Author { get; set; } = null!;
}

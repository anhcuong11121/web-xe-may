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

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public Guid AuthorId { get; set; }

    public virtual AppUser Author { get; set; } = null!;
}

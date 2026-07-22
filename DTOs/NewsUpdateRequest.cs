using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class NewsUpdateRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    [RegularExpression(@"^(https?://|/uploads/|/assets/).+", ErrorMessage = "Ảnh phải là URL HTTP(S) hoặc đường dẫn ảnh nội bộ.")]
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Published";

    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = "News";
}

using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductSkuId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? AltText { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }

    public virtual ProductSku ProductSku { get; set; } = null!;
}

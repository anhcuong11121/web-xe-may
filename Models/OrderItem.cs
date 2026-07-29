using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ProductSkuId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProductNameSnapshot { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string VariantNameSnapshot { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ColorNameSnapshot { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string SkuCodeSnapshot { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ProductSku ProductSku { get; set; } = null!;
}

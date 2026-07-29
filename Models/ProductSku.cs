using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class ProductSku
{
    public int Id { get; set; }

    public int ProductVariantId { get; set; }

    [Required]
    [MaxLength(64)]
    [RegularExpression(@"^[A-Z0-9]+(?:-[A-Z0-9]+)*$")]
    public string SkuCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ColorName { get; set; } = string.Empty;

    [MaxLength(9)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?$")]
    public string? ColorHexCode { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = CatalogStatuses.Active;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
}

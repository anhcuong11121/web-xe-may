using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Required]
    [MaxLength(100)]
    public string Color { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Available";

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public int BrandId { get; set; }

    public virtual Brand Brand { get; set; } = null!;

    public int? VehicleTypeId { get; set; }

    public virtual VehicleType? VehicleType { get; set; }

    public virtual Specification Specification { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();

    public virtual ICollection<ProductInterest> Interests { get; set; } = new List<ProductInterest>();

}

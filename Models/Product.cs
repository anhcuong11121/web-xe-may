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

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Available";

    public int BrandId { get; set; }

    public virtual Brand Brand { get; set; } = null!;

    public int? VehicleTypeId { get; set; }

    public virtual VehicleType? VehicleType { get; set; }

    public virtual ICollection<ProductInterest> Interests { get; set; } = new List<ProductInterest>();

    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

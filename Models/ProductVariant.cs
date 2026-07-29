using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    [RegularExpression(@"^[A-Z0-9]+(?:-[A-Z0-9]+)*$")]
    public string VersionCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = CatalogStatuses.Active;

    public virtual Product Product { get; set; } = null!;

    public virtual VariantSpecification? Specification { get; set; }

    public virtual ICollection<ProductSku> Skus { get; set; } = new List<ProductSku>();
}

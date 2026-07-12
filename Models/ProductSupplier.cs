namespace MotorBikeShop.API.Models;

public class ProductSupplier
{
    public int ProductId { get; set; }

    public int SupplierId { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}

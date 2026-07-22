namespace MotorBikeShop.API.Models;

public class ProductInterest
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    public virtual Product Product { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class Order
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    public virtual AppUser User { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Deposit> Deposits { get; set; } = new List<Deposit>();
}

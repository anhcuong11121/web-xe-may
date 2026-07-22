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

    [MaxLength(100)]
    public string? ReceiverName { get; set; }

    [MaxLength(20)]
    public string? ReceiverPhone { get; set; }

    [MaxLength(500)]
    public string? DeliveryAddress { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public Guid? ProcessedByUserId { get; set; }

    public virtual AppUser User { get; set; } = null!;

    public virtual AppUser? ProcessedBy { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Deposit? Deposit { get; set; }

    public virtual ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();
}

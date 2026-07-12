using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class Deposit
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime DepositDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    public virtual Order Order { get; set; } = null!;
}

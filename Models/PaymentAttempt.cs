using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public static class PaymentAttemptStatuses
{
    public const string Pending = "Pending";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Expired = "Expired";
}

public static class PaymentMethods
{
    public const string Demo = "Fake";
    public const string BankTransfer = "BankTransfer";
    public const string Cash = "Cash";
}

public class PaymentAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int OrderId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TransactionCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = PaymentAttemptStatuses.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid? ProcessedByUserId { get; set; }

    [MaxLength(1000)]
    public string? FailureReason { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual AppUser? ProcessedBy { get; set; }
}

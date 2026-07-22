using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class PaymentInitiateRequest
{
    [Required]
    public int OrderId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Fake";
}

public class PaymentAttemptQueryParameters
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public int? OrderId { get; set; }

    [RegularExpression("^(Pending|Succeeded|Failed|Expired)$")]
    public string? Status { get; set; }

    [RegularExpression("^(Fake|BankTransfer|Cash)$")]
    public string? PaymentMethod { get; set; }
}

public class PaymentAttemptDto
{
    public Guid Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public string? ProcessedByName { get; set; }
    public string? FailureReason { get; set; }
    public bool IsDemo { get; set; }
    public string ProcessingMode { get; set; } = string.Empty;
}

public class PaymentConfirmationDto
{
    public PaymentAttemptDto PaymentAttempt { get; set; } = new();
    public DepositDto Deposit { get; set; } = new();
}

public class PaymentConfigurationDto
{
    public string Mode { get; set; } = "Demo";
    public bool HasRealPaymentGateway { get; set; }
    public string Notice { get; set; } = string.Empty;
    public List<PaymentMethodConfigurationDto> Methods { get; set; } = new();
}

public class PaymentMethodConfigurationDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConfirmationType { get; set; } = string.Empty;
}

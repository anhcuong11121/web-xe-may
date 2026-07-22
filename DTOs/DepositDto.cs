namespace MotorBikeShop.API.DTOs;

public class DepositDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DepositDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}

namespace MotorBikeShop.API.DTOs;

public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class OrderDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Note { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public string? ProcessedByName { get; set; }
    public DepositDto? Deposit { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

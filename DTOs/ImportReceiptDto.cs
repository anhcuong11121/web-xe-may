namespace MotorBikeShop.API.DTOs;

public class ImportReceiptDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal => Quantity * UnitCost;
}

public class ImportReceiptDto
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public List<ImportReceiptDetailDto> Details { get; set; } = new();
}

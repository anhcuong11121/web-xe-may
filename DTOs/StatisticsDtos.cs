namespace MotorBikeShop.API.DTOs;

public class RevenueStatisticDto
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
}

public class OrderStatisticDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CustomerStatisticDto
{
    public int TotalCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
}

public class ProductStatisticDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
}

public class InventoryStatisticDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PurchaseStatisticDto
{
    public string Period { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int TotalVehicles { get; set; }
    public decimal TotalValue { get; set; }
}

public class ProductInterestStatisticDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int TotalQuantitySold { get; set; }
}

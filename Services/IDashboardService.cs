using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync();
    Task<List<RevenueStatisticDto>> GetRevenueStatisticsAsync(DateTime? from = null, DateTime? to = null);
    Task<List<OrderStatisticDto>> GetOrderStatisticsAsync(DateTime? from = null, DateTime? to = null);
    Task<CustomerStatisticDto> GetCustomerStatisticsAsync(DateTime? from = null, DateTime? to = null);
    Task<List<ProductStatisticDto>> GetProductStatisticsAsync(int top = 10, DateTime? from = null, DateTime? to = null);
    Task<List<InventoryStatisticDto>> GetInventoryStatisticsAsync();
    Task<List<PurchaseStatisticDto>> GetPurchaseStatisticsAsync(DateTime? from = null, DateTime? to = null);
    Task<List<ProductInterestStatisticDto>> GetProductInterestStatisticsAsync(int top = 10, DateTime? from = null, DateTime? to = null);
}

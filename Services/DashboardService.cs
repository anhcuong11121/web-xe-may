using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class DashboardService : IDashboardService
{
    private const string CompletedStatus = "Completed";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public DashboardService(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var totalRevenue = await _context.Orders
            .Where(o => o.Status == CompletedStatus)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var totalOrders = await _context.Orders.CountAsync();
        var totalCustomers = (await _userManager.GetUsersInRoleAsync("Customer")).Count;
        var totalProducts = await _context.Products.CountAsync();

        return new DashboardDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts
        };
    }

    public async Task<List<RevenueStatisticDto>> GetRevenueStatisticsAsync(DateTime? from = null, DateTime? to = null)
    {
        var orders = await FilterOrders(from, to)
            .Where(order => order.Status == CompletedStatus)
            .ToListAsync();

        return orders
            .GroupBy(o => o.OrderDate.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new RevenueStatisticDto
            {
                Period = g.Key,
                TotalRevenue = g.Sum(o => o.TotalAmount)
            })
            .ToList();
    }

    public async Task<List<OrderStatisticDto>> GetOrderStatisticsAsync(DateTime? from = null, DateTime? to = null)
    {
        var stats = await FilterOrders(from, to)
            .GroupBy(o => o.Status)
            .Select(g => new OrderStatisticDto
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        return stats;
    }

    public async Task<CustomerStatisticDto> GetCustomerStatisticsAsync(DateTime? from = null, DateTime? to = null)
    {
        var customers = await _userManager.GetUsersInRoleAsync("Customer");
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var filteredCustomers = customers.Where(customer => IsInRange(customer.CreatedAt, from, to)).ToList();

        return new CustomerStatisticDto
        {
            TotalCustomers = filteredCustomers.Count,
            NewCustomersThisMonth = filteredCustomers.Count(customer => customer.CreatedAt >= startOfMonth)
        };
    }

    public async Task<List<ProductStatisticDto>> GetProductStatisticsAsync(int top = 10, DateTime? from = null, DateTime? to = null)
    {
        top = Math.Clamp(top, 1, 100);
        var query = _context.OrderItems.AsQueryable();
        if (from.HasValue) query = query.Where(item => item.Order.OrderDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(item => item.Order.OrderDate < to.Value.Date.AddDays(1));

        var stats = await query
            .GroupBy(oi => new
            {
                ProductId = oi.ProductSku.ProductVariant.ProductId,
                oi.ProductSku.ProductVariant.Product.Name
            })
            .Select(g => new ProductStatisticDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalQuantitySold = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .Take(top)
            .ToListAsync();

        return stats;
    }

    public Task<List<InventoryStatisticDto>> GetInventoryStatisticsAsync()
    {
        return _context.Products.AsNoTracking()
            .Select(product => new
            {
                Product = product,
                StockQuantity = product.Variants
                    .Where(variant => variant.Status == CatalogStatuses.Active)
                    .SelectMany(variant => variant.Skus)
                    .Where(sku => sku.Status == CatalogStatuses.Active)
                    .Sum(sku => (int?)sku.StockQuantity) ?? 0
            })
            .OrderBy(item => item.StockQuantity)
            .ThenBy(item => item.Product.Name)
            .Select(item => new InventoryStatisticDto
            {
                ProductId = item.Product.Id,
                ProductName = item.Product.Name,
                StockQuantity = item.StockQuantity,
                Status = item.StockQuantity == 0
                    ? "OutOfStock"
                    : item.StockQuantity <= 5
                        ? "LowStock"
                        : "InStock"
            })
            .ToListAsync();
    }

    public async Task<List<PurchaseStatisticDto>> GetPurchaseStatisticsAsync(DateTime? from = null, DateTime? to = null)
    {
        var orders = await FilterOrders(from, to)
            .Include(order => order.OrderItems)
            .ToListAsync();
        return orders.GroupBy(order => order.OrderDate.ToString("yyyy-MM"))
            .OrderBy(group => group.Key)
            .Select(group => new PurchaseStatisticDto
            {
                Period = group.Key,
                TotalOrders = group.Count(),
                TotalVehicles = group.Sum(order => order.OrderItems.Sum(item => item.Quantity)),
                TotalValue = group.Sum(order => order.TotalAmount)
            })
            .ToList();
    }

    public async Task<List<ProductInterestStatisticDto>> GetProductInterestStatisticsAsync(
        int top = 10, DateTime? from = null, DateTime? to = null)
    {
        top = Math.Clamp(top, 1, 100);
        var interestQuery = _context.ProductInterests.AsNoTracking().AsQueryable();
        var soldQuery = _context.OrderItems.AsNoTracking().AsQueryable();
        if (from.HasValue)
        {
            interestQuery = interestQuery.Where(interest => interest.ViewedAt >= from.Value.Date);
            soldQuery = soldQuery.Where(item => item.Order.OrderDate >= from.Value.Date);
        }
        if (to.HasValue)
        {
            var exclusiveTo = to.Value.Date.AddDays(1);
            interestQuery = interestQuery.Where(interest => interest.ViewedAt < exclusiveTo);
            soldQuery = soldQuery.Where(item => item.Order.OrderDate < exclusiveTo);
        }

        return await _context.Products.AsNoTracking()
            .Select(product => new ProductInterestStatisticDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ViewCount = interestQuery.Count(interest => interest.ProductId == product.Id),
                TotalQuantitySold = soldQuery
                    .Where(item => item.ProductSku.ProductVariant.ProductId == product.Id)
                    .Sum(item => (int?)item.Quantity) ?? 0
            })
            .Where(item => item.ViewCount > 0 || item.TotalQuantitySold > 0)
            .OrderByDescending(item => item.ViewCount)
            .ThenByDescending(item => item.TotalQuantitySold)
            .Take(top)
            .ToListAsync();
    }

    private IQueryable<Order> FilterOrders(DateTime? from, DateTime? to)
    {
        var query = _context.Orders.AsNoTracking();
        if (from.HasValue) query = query.Where(order => order.OrderDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(order => order.OrderDate < to.Value.Date.AddDays(1));
        return query;
    }

    private static bool IsInRange(DateTime value, DateTime? from, DateTime? to) =>
        (!from.HasValue || value >= from.Value.Date) &&
        (!to.HasValue || value < to.Value.Date.AddDays(1));
}

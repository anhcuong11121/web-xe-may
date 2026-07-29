using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class OrderService : IOrderService
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedManualTransitions =
        new Dictionary<string, string[]>
        {
            ["Pending"] = new[] { "Cancelled" },
            ["Deposited"] = new[] { "Confirmed" },
            ["Confirmed"] = new[] { "Processing" },
            ["Processing"] = new[] { "Completed" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        };

    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<OrderDto>> CreateOrderAsync(Guid userId, OrderCreateRequest request)
    {
        if (request.ExpectedDeliveryDate.Date < DateTime.UtcNow.Date)
        {
            return ServiceResult<OrderDto>.Fail("Ngày hẹn nhận xe không được nằm trong quá khứ.");
        }

        if (request.Items.Count == 0)
        {
            return ServiceResult<OrderDto>.Fail("Đơn hàng phải có ít nhất 1 sản phẩm.");
        }

        if (request.Items.Any(item => item.ProductSkuId <= 0 || item.Quantity <= 0))
        {
            return ServiceResult<OrderDto>.Fail("SKU và số lượng trong đơn hàng không hợp lệ.");
        }

        var normalizedItems = request.Items
            .GroupBy(item => item.ProductSkuId)
            .Select(group => new
            {
                ProductSkuId = group.Key,
                Quantity = group.Sum(item => (long)item.Quantity)
            })
            .OrderBy(item => item.ProductSkuId)
            .ToList();

        if (normalizedItems.Any(item => item.Quantity > int.MaxValue))
        {
            return ServiceResult<OrderDto>.Fail("Tổng số lượng sản phẩm vượt giới hạn cho phép.");
        }

        var skuIds = normalizedItems.Select(item => item.ProductSkuId).ToList();
        var isRelationalDatabase = _context.Database.IsRelational();
        IDbContextTransaction? transaction = null;
        if (isRelationalDatabase)
        {
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        }

        await using var transactionScope = transaction;
        var skuQuery = _context.ProductSkus
            .Include(sku => sku.ProductVariant)
                .ThenInclude(variant => variant.Product)
            .Where(sku => skuIds.Contains(sku.Id));
        var skus = isRelationalDatabase
            ? await skuQuery.AsNoTracking().ToListAsync()
            : await skuQuery.ToListAsync();

        if (skus.Count != skuIds.Count)
        {
            return ServiceResult<OrderDto>.Fail("Có SKU không tồn tại trong đơn hàng.");
        }

        foreach (var item in normalizedItems)
        {
            var sku = skus.First(candidate => candidate.Id == item.ProductSkuId);
            if (sku.Status != CatalogStatuses.Active ||
                sku.ProductVariant.Status != CatalogStatuses.Active)
            {
                return ServiceResult<OrderDto>.Fail(
                    $"SKU '{sku.SkuCode}' hiện không được bán.");
            }

            if (sku.StockQuantity < item.Quantity)
            {
                return ServiceResult<OrderDto>.Fail(
                    $"SKU '{sku.SkuCode}' không đủ tồn kho (còn {sku.StockQuantity}).");
            }
        }

        if (isRelationalDatabase)
        {
            foreach (var item in normalizedItems)
            {
                var quantity = (int)item.Quantity;
                var updated = await _context.ProductSkus
                    .Where(sku =>
                        sku.Id == item.ProductSkuId &&
                        sku.Status == CatalogStatuses.Active &&
                        sku.ProductVariant.Status == CatalogStatuses.Active &&
                        sku.StockQuantity >= quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        sku => sku.StockQuantity,
                        sku => sku.StockQuantity - quantity));
                if (updated == 0)
                {
                    await transaction!.RollbackAsync();
                    return ServiceResult<OrderDto>.Fail(
                        "Tồn kho SKU vừa thay đổi bởi đơn hàng khác. Vui lòng kiểm tra và thử lại.");
                }
            }

        }

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            ReceiverName = request.ReceiverName.Trim(),
            ReceiverPhone = request.ReceiverPhone.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            Note = request.Note?.Trim(),
            ExpectedDeliveryDate = request.ExpectedDeliveryDate.Date
        };

        decimal totalAmount = 0m;

        foreach (var item in normalizedItems)
        {
            var sku = skus.First(candidate => candidate.Id == item.ProductSkuId);
            var variant = sku.ProductVariant;
            var product = variant.Product;
            var quantity = (int)item.Quantity;
            var orderItem = new OrderItem
            {
                ProductSkuId = sku.Id,
                ProductNameSnapshot = product.Name,
                VariantNameSnapshot = variant.Name,
                ColorNameSnapshot = sku.ColorName,
                SkuCodeSnapshot = sku.SkuCode,
                Quantity = quantity,
                UnitPrice = sku.Price
            };

            order.OrderItems.Add(orderItem);
            totalAmount += sku.Price * quantity;
            if (!isRelationalDatabase)
            {
                sku.StockQuantity -= quantity;
            }
        }

        order.TotalAmount = totalAmount;

        try
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch (DbUpdateException)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }

            _context.ChangeTracker.Clear();
            return ServiceResult<OrderDto>.Fail(
                "Không thể tạo đơn hàng; toàn bộ thay đổi tồn kho đã được hoàn tác.");
        }

        await _context.Entry(order).Reference(o => o.User).LoadAsync();
        return ServiceResult<OrderDto>.Success(MapToDto(order));
    }

    public async Task<List<OrderDto>> GetOrdersAsync(Guid currentUserId, string currentUserRole)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.ProcessedBy)
            .Include(o => o.Deposit)
            .Include(o => o.OrderItems)
            .AsSplitQuery()
            .AsQueryable();

        if (currentUserRole is not ("Employee" or "Admin"))
        {
            query = query.Where(o => o.UserId == currentUserId);
        }

        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id, Guid currentUserId, string currentUserRole)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.ProcessedBy)
            .Include(o => o.Deposit)
            .Include(o => o.OrderItems)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return null;
        }

        if (currentUserRole is not ("Employee" or "Admin") && order.UserId != currentUserId)
        {
            return null;
        }

        return MapToDto(order);
    }

    public async Task<ServiceResult<OrderDto>> UpdateOrderStatusAsync(Guid processedByUserId, OrderStatusUpdateRequest request)
    {
        var isRelationalDatabase = _context.Database.IsRelational();
        IDbContextTransaction? transaction = null;
        if (isRelationalDatabase)
        {
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        }

        await using var transactionScope = transaction;
        var orderQuery = _context.Orders
            .Include(o => o.User)
            .Include(o => o.ProcessedBy)
            .Include(o => o.Deposit)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.ProductSku)
            .AsSplitQuery();
        var order = isRelationalDatabase
            ? await orderQuery.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.OrderId)
            : await orderQuery.FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order == null)
        {
            return ServiceResult<OrderDto>.Fail("Không tìm thấy đơn hàng.");
        }

        if (!AllowedManualTransitions.TryGetValue(order.Status, out var allowedTargets) ||
            !allowedTargets.Contains(request.Status))
        {
            var allowedText = allowedTargets == null || allowedTargets.Length == 0
                ? "không còn trạng thái kế tiếp"
                : string.Join(", ", allowedTargets);
            return ServiceResult<OrderDto>.Fail(
                $"Không thể chuyển trạng thái từ {order.Status} sang {request.Status}. Cho phép: {allowedText}.");
        }

        if (isRelationalDatabase)
        {
            var updated = await _context.Orders
                .Where(candidate => candidate.Id == order.Id && candidate.Status == order.Status)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.Status, request.Status)
                    .SetProperty(candidate => candidate.ProcessedByUserId, processedByUserId));
            if (updated == 0)
            {
                await transaction!.RollbackAsync();
                return ServiceResult<OrderDto>.Fail(
                    "Đơn hàng vừa được xử lý bởi request khác. Vui lòng tải lại trạng thái.");
            }

            if (request.Status == "Cancelled")
            {
                var cancelledAt = DateTime.UtcNow;
                await _context.PaymentAttempts
                    .Where(attempt =>
                        attempt.OrderId == order.Id &&
                        attempt.Status == PaymentAttemptStatuses.Pending)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(attempt => attempt.Status, PaymentAttemptStatuses.Failed)
                        .SetProperty(attempt => attempt.CompletedAt, cancelledAt)
                        .SetProperty(attempt => attempt.FailureReason, "Đơn hàng đã bị hủy."));

                foreach (var itemGroup in order.OrderItems
                             .GroupBy(item => item.ProductSkuId)
                             .OrderBy(group => group.Key))
                {
                    var restoredQuantity = itemGroup.Sum(item => item.Quantity);
                    var maximumCurrentStock = int.MaxValue - restoredQuantity;
                    var restored = await _context.ProductSkus
                        .Where(sku =>
                            sku.Id == itemGroup.Key &&
                            sku.StockQuantity <= maximumCurrentStock)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(
                            sku => sku.StockQuantity,
                            sku => sku.StockQuantity + restoredQuantity));
                    if (restored == 0)
                    {
                        await transaction!.RollbackAsync();
                        return ServiceResult<OrderDto>.Fail(
                            "Không thể hoàn tồn SKU vì số lượng vượt giới hạn cho phép.");
                    }
                }

            }

            await transaction!.CommitAsync();
            var updatedOrder = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.ProcessedBy)
                .Include(o => o.Deposit)
                .Include(o => o.OrderItems)
                .FirstAsync(o => o.Id == request.OrderId);
            return ServiceResult<OrderDto>.Success(MapToDto(updatedOrder));
        }

        if (request.Status == "Cancelled")
        {
            var cancelledAt = DateTime.UtcNow;
            var pendingAttempts = await _context.PaymentAttempts
                .Where(attempt =>
                    attempt.OrderId == order.Id &&
                    attempt.Status == PaymentAttemptStatuses.Pending)
                .ToListAsync();
            foreach (var attempt in pendingAttempts)
            {
                attempt.Status = PaymentAttemptStatuses.Failed;
                attempt.CompletedAt = cancelledAt;
                attempt.FailureReason = "Đơn hàng đã bị hủy.";
            }

            foreach (var item in order.OrderItems)
            {
                item.ProductSku.StockQuantity += item.Quantity;
            }
        }

        order.Status = request.Status;
        order.ProcessedByUserId = processedByUserId;
        await _context.SaveChangesAsync();
        order.ProcessedBy = await _context.Users.FindAsync(processedByUserId);

        return ServiceResult<OrderDto>.Success(MapToDto(order));
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            UserFullName = order.User?.FullName ?? string.Empty,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            DeliveryAddress = order.DeliveryAddress,
            Note = order.Note,
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            ProcessedByUserId = order.ProcessedByUserId,
            ProcessedByName = order.ProcessedBy?.FullName,
            Deposit = order.Deposit == null ? null : new DepositDto
            {
                Id = order.Deposit.Id,
                OrderId = order.Deposit.OrderId,
                Amount = order.Deposit.Amount,
                DepositDate = order.Deposit.DepositDate,
                PaymentMethod = order.Deposit.PaymentMethod,
                TransactionCode = order.Deposit.TransactionCode,
                Status = order.Deposit.Status,
                PaidAt = order.Deposit.PaidAt
            },
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                ProductSkuId = oi.ProductSkuId,
                ProductName = oi.ProductNameSnapshot,
                VariantName = oi.VariantNameSnapshot,
                ColorName = oi.ColorNameSnapshot,
                SkuCode = oi.SkuCodeSnapshot,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        };
    }
}

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

        if (request.Items.Any(item => item.ProductId <= 0 || item.Quantity <= 0))
        {
            return ServiceResult<OrderDto>.Fail("Sản phẩm và số lượng trong đơn hàng không hợp lệ.");
        }

        var normalizedItems = request.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => (long)item.Quantity)
            })
            .ToList();

        if (normalizedItems.Any(item => item.Quantity > int.MaxValue))
        {
            return ServiceResult<OrderDto>.Fail("Tổng số lượng sản phẩm vượt giới hạn cho phép.");
        }

        var productIds = normalizedItems.Select(item => item.ProductId).ToList();
        var isRelationalDatabase = _context.Database.IsRelational();
        IDbContextTransaction? transaction = null;
        if (isRelationalDatabase)
        {
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        }

        await using var transactionScope = transaction;
        var productQuery = _context.Products.Where(product => productIds.Contains(product.Id));
        var products = isRelationalDatabase
            ? await productQuery.AsNoTracking().ToListAsync()
            : await productQuery.ToListAsync();

        if (products.Count != productIds.Count)
        {
            return ServiceResult<OrderDto>.Fail("Có sản phẩm không tồn tại trong đơn hàng.");
        }

        foreach (var item in normalizedItems)
        {
            var product = products.First(p => p.Id == item.ProductId);
            if (product.StockQuantity < item.Quantity)
            {
                return ServiceResult<OrderDto>.Fail($"Sản phẩm '{product.Name}' không đủ tồn kho (còn {product.StockQuantity}).");
            }
        }

        if (isRelationalDatabase)
        {
            foreach (var item in normalizedItems)
            {
                var quantity = (int)item.Quantity;
                var updated = await _context.Products
                    .Where(product => product.Id == item.ProductId && product.StockQuantity >= quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        product => product.StockQuantity,
                        product => product.StockQuantity - quantity));
                if (updated == 0)
                {
                    await transaction!.RollbackAsync();
                    return ServiceResult<OrderDto>.Fail(
                        "Tồn kho vừa thay đổi bởi đơn hàng khác. Vui lòng kiểm tra và thử lại.");
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
            var product = products.First(p => p.Id == item.ProductId);
            var quantity = (int)item.Quantity;
            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price
            };

            order.OrderItems.Add(orderItem);
            totalAmount += product.Price * quantity;
            if (!isRelationalDatabase)
            {
                product.StockQuantity -= quantity;
            }
        }

        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }

        await _context.Entry(order).Reference(o => o.User).LoadAsync();
        foreach (var orderItem in order.OrderItems)
        {
            await _context.Entry(orderItem).Reference(oi => oi.Product).LoadAsync();
        }

        return ServiceResult<OrderDto>.Success(MapToDto(order));
    }

    public async Task<List<OrderDto>> GetOrdersAsync(Guid currentUserId, string currentUserRole)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.ProcessedBy)
            .Include(o => o.Deposit)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
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
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
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
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product);
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

                foreach (var itemGroup in order.OrderItems.GroupBy(item => item.ProductId))
                {
                    var restoredQuantity = itemGroup.Sum(item => item.Quantity);
                    await _context.Products
                        .Where(product => product.Id == itemGroup.Key)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(
                            product => product.StockQuantity,
                            product => product.StockQuantity + restoredQuantity));
                }
            }

            await transaction!.CommitAsync();
            var updatedOrder = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.ProcessedBy)
                .Include(o => o.Deposit)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
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
                item.Product.StockQuantity += item.Quantity;
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
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? string.Empty,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        };
    }
}

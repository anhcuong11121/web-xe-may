using System.Data;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class PaymentService : IPaymentService
{
    private static readonly string[] AllowedPaymentMethods = { PaymentMethods.Demo, PaymentMethods.BankTransfer, PaymentMethods.Cash };
    private static readonly TimeSpan AttemptLifetime = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _context;

    public PaymentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<PaymentAttemptDto>> InitiateAsync(
        Guid currentUserId,
        PaymentInitiateRequest request)
    {
        if (request.Amount <= 0)
        {
            return ServiceResult<PaymentAttemptDto>.Fail("Số tiền đặt cọc phải lớn hơn 0.");
        }

        if (!AllowedPaymentMethods.Contains(request.PaymentMethod))
        {
            return ServiceResult<PaymentAttemptDto>.Fail(
                $"Phương thức thanh toán không hợp lệ. Cho phép: {string.Join(", ", AllowedPaymentMethods)}.");
        }

        var order = await _context.Orders
            .Include(o => o.Deposit)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == currentUserId);
        if (order == null)
        {
            return ServiceResult<PaymentAttemptDto>.Fail("Không tìm thấy đơn hàng.");
        }

        if (order.Deposit != null || order.Status != "Pending")
        {
            return ServiceResult<PaymentAttemptDto>.Fail("Đơn hàng không còn ở trạng thái chờ đặt cọc.");
        }

        if (request.Amount > order.TotalAmount)
        {
            return ServiceResult<PaymentAttemptDto>.Fail("Số tiền đặt cọc không được vượt quá tổng tiền đơn hàng.");
        }

        var now = DateTime.UtcNow;
        var pendingAttempts = await _context.PaymentAttempts
            .Where(p => p.OrderId == order.Id && p.Status == PaymentAttemptStatuses.Pending)
            .ToListAsync();

        var expiredAttempts = pendingAttempts.Where(p => p.ExpiresAt <= now).ToList();
        foreach (var expiredAttempt in expiredAttempts)
        {
            expiredAttempt.Status = PaymentAttemptStatuses.Expired;
            expiredAttempt.CompletedAt = now;
            expiredAttempt.FailureReason = "Phiên thanh toán đã hết hạn.";
        }

        if (expiredAttempts.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        var activeAttempt = pendingAttempts.FirstOrDefault(p => p.ExpiresAt > now);
        if (activeAttempt != null)
        {
            if (activeAttempt.Amount == request.Amount && activeAttempt.PaymentMethod == request.PaymentMethod)
            {
                return ServiceResult<PaymentAttemptDto>.Success(Map(activeAttempt));
            }

            return ServiceResult<PaymentAttemptDto>.Fail(
                "Đơn hàng đang có một phiên thanh toán còn hiệu lực với thông tin khác.");
        }

        var attempt = new PaymentAttempt
        {
            OrderId = order.Id,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            TransactionCode = $"PAY-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}",
            Status = PaymentAttemptStatuses.Pending,
            CreatedAt = now,
            ExpiresAt = now.Add(AttemptLifetime)
        };

        _context.PaymentAttempts.Add(attempt);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            _context.Entry(attempt).State = EntityState.Detached;
            var concurrentAttempt = await _context.PaymentAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.OrderId == order.Id && p.Status == PaymentAttemptStatuses.Pending);

            if (concurrentAttempt != null &&
                concurrentAttempt.Amount == request.Amount &&
                concurrentAttempt.PaymentMethod == request.PaymentMethod)
            {
                return ServiceResult<PaymentAttemptDto>.Success(Map(concurrentAttempt));
            }

            return ServiceResult<PaymentAttemptDto>.Fail(
                "Không thể tạo phiên thanh toán vì đơn hàng đang được xử lý bởi một request khác.");
        }

        return ServiceResult<PaymentAttemptDto>.Success(Map(attempt));
    }

    public async Task<PaymentAttemptDto?> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole)
    {
        var query = _context.PaymentAttempts
            .Include(p => p.Order)
            .Include(p => p.ProcessedBy)
            .Where(p => p.Id == id);

        if (currentUserRole is not ("Employee" or "Admin"))
        {
            query = query.Where(p => p.Order.UserId == currentUserId);
        }

        var attempt = await query.FirstOrDefaultAsync();
        if (attempt?.Status == PaymentAttemptStatuses.Pending && attempt.ExpiresAt <= DateTime.UtcNow)
        {
            var completedAt = DateTime.UtcNow;
            var updated = await _context.PaymentAttempts
                .Where(p => p.Id == attempt.Id &&
                            p.Status == PaymentAttemptStatuses.Pending &&
                            p.ExpiresAt <= completedAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, PaymentAttemptStatuses.Expired)
                    .SetProperty(p => p.CompletedAt, completedAt)
                    .SetProperty(p => p.FailureReason, "Phiên thanh toán đã hết hạn."));

            if (updated > 0)
            {
                attempt.Status = PaymentAttemptStatuses.Expired;
                attempt.CompletedAt = completedAt;
                attempt.FailureReason = "Phiên thanh toán đã hết hạn.";
            }
            else
            {
                attempt = await query.AsNoTracking().FirstOrDefaultAsync();
            }
        }

        return attempt == null ? null : Map(attempt);
    }

    public async Task<PagedResult<PaymentAttemptDto>> GetListAsync(
        PaymentAttemptQueryParameters parameters,
        Guid currentUserId,
        string currentUserRole)
    {
        var query = _context.PaymentAttempts
            .AsNoTracking()
            .Include(p => p.Order)
            .Include(p => p.ProcessedBy)
            .AsQueryable();

        if (currentUserRole is not ("Employee" or "Admin"))
        {
            query = query.Where(p => p.Order.UserId == currentUserId);
        }

        if (parameters.OrderId.HasValue)
        {
            query = query.Where(p => p.OrderId == parameters.OrderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            query = query.Where(p => p.Status == parameters.Status);
        }

        if (!string.IsNullOrWhiteSpace(parameters.PaymentMethod))
        {
            query = query.Where(p => p.PaymentMethod == parameters.PaymentMethod);
        }

        var totalCount = await query.CountAsync();
        var attempts = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        return new PagedResult<PaymentAttemptDto>
        {
            Items = attempts.Select(Map).ToList(),
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }

    public async Task<ServiceResult<PaymentAttemptDto>> FailFakeAsync(Guid id, Guid currentUserId)
    {
        var attempt = await _context.PaymentAttempts
            .AsNoTracking()
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id && p.Order.UserId == currentUserId);
        if (attempt == null)
        {
            return ServiceResult<PaymentAttemptDto>.Fail("Không tìm thấy phiên thanh toán.");
        }

        if (attempt.Status == PaymentAttemptStatuses.Failed)
        {
            return ServiceResult<PaymentAttemptDto>.Success(Map(attempt));
        }

        if (attempt.PaymentMethod != PaymentMethods.Demo)
        {
            return ServiceResult<PaymentAttemptDto>.Fail(
                "Chỉ phiên Fake mới được mô phỏng kết quả thất bại.");
        }

        var now = DateTime.UtcNow;
        if (attempt.ExpiresAt <= now)
        {
            await _context.PaymentAttempts
                .Where(p => p.Id == id && p.Status == PaymentAttemptStatuses.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, PaymentAttemptStatuses.Expired)
                    .SetProperty(p => p.CompletedAt, now)
                    .SetProperty(p => p.FailureReason, "Phiên thanh toán đã hết hạn."));
            return ServiceResult<PaymentAttemptDto>.Fail("Phiên thanh toán đã hết hạn.");
        }

        var updated = await _context.PaymentAttempts
            .Where(p => p.Id == id &&
                        p.Status == PaymentAttemptStatuses.Pending &&
                        p.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, PaymentAttemptStatuses.Failed)
                .SetProperty(p => p.CompletedAt, now)
                .SetProperty(p => p.FailureReason, "Thanh toán giả lập thất bại."));

        var current = await _context.PaymentAttempts.AsNoTracking().FirstAsync(p => p.Id == id);
        if (updated == 0 && current.Status != PaymentAttemptStatuses.Failed)
        {
            return ServiceResult<PaymentAttemptDto>.Fail(
                $"Không thể đánh dấu thất bại vì phiên đang ở trạng thái {current.Status}.");
        }

        return ServiceResult<PaymentAttemptDto>.Success(Map(current));
    }

    public async Task<ServiceResult<PaymentConfirmationDto>> ConfirmFakeAsync(
        Guid id,
        Guid currentUserId)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var attempt = await _context.PaymentAttempts
            .Include(p => p.Order)
            .ThenInclude(o => o.Deposit)
            .FirstOrDefaultAsync(p => p.Id == id && p.Order.UserId == currentUserId);
        if (attempt == null)
        {
            return ServiceResult<PaymentConfirmationDto>.Fail("Không tìm thấy phiên thanh toán.");
        }

        if (attempt.Status == PaymentAttemptStatuses.Succeeded && attempt.Order.Deposit != null)
        {
            await transaction.CommitAsync();
            return ServiceResult<PaymentConfirmationDto>.Success(
                MapConfirmation(attempt, attempt.Order.Deposit));
        }

        if (attempt.Status != PaymentAttemptStatuses.Pending)
        {
            return ServiceResult<PaymentConfirmationDto>.Fail(
                $"Không thể xác nhận phiên thanh toán ở trạng thái {attempt.Status}.");
        }

        var now = DateTime.UtcNow;
        if (attempt.ExpiresAt <= now)
        {
            attempt.Status = PaymentAttemptStatuses.Expired;
            attempt.CompletedAt = now;
            attempt.FailureReason = "Phiên thanh toán đã hết hạn.";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ServiceResult<PaymentConfirmationDto>.Fail(attempt.FailureReason);
        }

        if (attempt.PaymentMethod != PaymentMethods.Demo)
        {
            return ServiceResult<PaymentConfirmationDto>.Fail(
                "Chỉ phiên Fake mới được xác nhận bằng endpoint giả lập.");
        }

        if (attempt.Order.Status != "Pending" || attempt.Order.Deposit != null)
        {
            return ServiceResult<PaymentConfirmationDto>.Fail(
                "Đơn hàng không còn ở trạng thái chờ đặt cọc.");
        }

        var deposit = new Deposit
        {
            OrderId = attempt.OrderId,
            Amount = attempt.Amount,
            DepositDate = now,
            PaymentMethod = attempt.PaymentMethod,
            TransactionCode = attempt.TransactionCode,
            Status = "Completed",
            PaidAt = now
        };

        attempt.Status = PaymentAttemptStatuses.Succeeded;
        attempt.CompletedAt = now;
        attempt.FailureReason = null;
        attempt.Order.Status = "Deposited";
        _context.Deposits.Add(deposit);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult<PaymentConfirmationDto>.Success(MapConfirmation(attempt, deposit));
    }

    public async Task<ServiceResult<PaymentConfirmationDto>> CompleteManualAsync(
        Guid id,
        Guid processedByUserId)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var attempt = await _context.PaymentAttempts
            .Include(p => p.Order)
            .ThenInclude(o => o.Deposit)
            .Include(p => p.ProcessedBy)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (attempt == null)
        {
            return ServiceResult<PaymentConfirmationDto>.Fail("Không tìm thấy phiên thanh toán.");
        }

        if (attempt.Status == PaymentAttemptStatuses.Succeeded && attempt.Order.Deposit != null)
        {
            await transaction.CommitAsync();
            return ServiceResult<PaymentConfirmationDto>.Success(
                MapConfirmation(attempt, attempt.Order.Deposit));
        }

        if (attempt.Status != PaymentAttemptStatuses.Pending)
        {
            return ServiceResult<PaymentConfirmationDto>.Fail(
                $"Không thể hoàn tất phiên thanh toán ở trạng thái {attempt.Status}.");
        }

        var now = DateTime.UtcNow;
        if (attempt.ExpiresAt <= now)
        {
            attempt.Status = PaymentAttemptStatuses.Expired;
            attempt.CompletedAt = now;
            attempt.FailureReason = "Phiên thanh toán đã hết hạn.";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ServiceResult<PaymentConfirmationDto>.Fail(attempt.FailureReason);
        }

        if (attempt.PaymentMethod is not (PaymentMethods.BankTransfer or PaymentMethods.Cash))
        {
            return ServiceResult<PaymentConfirmationDto>.Fail(
                "Chỉ phiên BankTransfer hoặc Cash mới được nhân viên hoàn tất thủ công.");
        }

        if (attempt.Order.Status != "Pending" || attempt.Order.Deposit != null)
        {
            return ServiceResult<PaymentConfirmationDto>.Fail(
                "Đơn hàng không còn ở trạng thái chờ đặt cọc.");
        }

        var processor = await _context.Users.FindAsync(processedByUserId);
        if (processor == null)
        {
            return ServiceResult<PaymentConfirmationDto>.Fail("Không tìm thấy người xử lý thanh toán.");
        }

        var deposit = new Deposit
        {
            OrderId = attempt.OrderId,
            Amount = attempt.Amount,
            DepositDate = now,
            PaymentMethod = attempt.PaymentMethod,
            TransactionCode = attempt.TransactionCode,
            Status = "Completed",
            PaidAt = now
        };

        attempt.Status = PaymentAttemptStatuses.Succeeded;
        attempt.CompletedAt = now;
        attempt.ProcessedByUserId = processedByUserId;
        attempt.ProcessedBy = processor;
        attempt.FailureReason = null;
        attempt.Order.Status = "Deposited";
        _context.Deposits.Add(deposit);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult<PaymentConfirmationDto>.Success(MapConfirmation(attempt, deposit));
    }

    private static PaymentAttemptDto Map(PaymentAttempt attempt) => new()
    {
        Id = attempt.Id,
        OrderId = attempt.OrderId,
        Amount = attempt.Amount,
        PaymentMethod = attempt.PaymentMethod,
        TransactionCode = attempt.TransactionCode,
        Status = attempt.Status,
        CreatedAt = attempt.CreatedAt,
        ExpiresAt = attempt.ExpiresAt,
        CompletedAt = attempt.CompletedAt,
        ProcessedByUserId = attempt.ProcessedByUserId,
        ProcessedByName = attempt.ProcessedBy?.FullName,
        FailureReason = attempt.FailureReason,
        IsDemo = attempt.PaymentMethod == PaymentMethods.Demo,
        ProcessingMode = attempt.PaymentMethod == PaymentMethods.Demo ? "Simulated" : "ManualConfirmation"
    };

    private static PaymentConfirmationDto MapConfirmation(PaymentAttempt attempt, Deposit deposit) => new()
    {
        PaymentAttempt = Map(attempt),
        Deposit = new DepositDto
        {
            Id = deposit.Id,
            OrderId = deposit.OrderId,
            Amount = deposit.Amount,
            DepositDate = deposit.DepositDate,
            PaymentMethod = deposit.PaymentMethod,
            TransactionCode = deposit.TransactionCode,
            Status = deposit.Status,
            PaidAt = deposit.PaidAt
        }
    };
}

using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class SupportRequestService : ISupportRequestService
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>
        {
            ["Open"] = new[] { "InProgress", "Closed" },
            ["InProgress"] = new[] { "Resolved", "Closed" },
            ["Resolved"] = new[] { "Closed" },
            ["Closed"] = Array.Empty<string>()
        };

    private readonly ApplicationDbContext _context;

    public SupportRequestService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupportRequestDto> CreateAsync(Guid userId, SupportRequestCreateRequest request)
    {
        var supportRequest = new SupportRequest
        {
            UserId = userId,
            SupportType = request.SupportType.Trim(),
            Subject = request.Subject,
            Message = request.Message,
            CreatedAt = DateTime.UtcNow,
            Status = "Open"
        };

        _context.SupportRequests.Add(supportRequest);
        await _context.SaveChangesAsync();

        await _context.Entry(supportRequest).Reference(s => s.User).LoadAsync();

        return MapToDto(supportRequest);
    }

    public async Task<List<SupportRequestDto>> GetRequestsAsync(Guid currentUserId, string currentUserRole)
    {
        var query = _context.SupportRequests
            .Include(s => s.User)
            .Include(s => s.AssignedEmployee)
            .AsQueryable();

        if (currentUserRole is not ("Employee" or "Admin"))
        {
            query = query.Where(s => s.UserId == currentUserId);
        }

        var requests = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return requests.Select(MapToDto).ToList();
    }

    public async Task<SupportRequestDto?> GetByIdAsync(int id, Guid currentUserId, string currentUserRole)
    {
        var request = await _context.SupportRequests
            .Include(s => s.User)
            .Include(s => s.AssignedEmployee)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (request == null)
        {
            return null;
        }

        if (currentUserRole is not ("Employee" or "Admin") && request.UserId != currentUserId)
        {
            return null;
        }

        return MapToDto(request);
    }

    public async Task<ServiceResult<SupportRequestDto>> UpdateAsync(
        int id,
        Guid assignedEmployeeUserId,
        SupportRequestUpdateRequest request)
    {
        var supportRequest = await _context.SupportRequests
            .Include(s => s.User)
            .Include(s => s.AssignedEmployee)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (supportRequest == null)
        {
            return ServiceResult<SupportRequestDto>.Fail("Không tìm thấy yêu cầu chăm sóc khách hàng.");
        }

        if (!AllowedTransitions.TryGetValue(supportRequest.Status, out var allowedTargets) ||
            !allowedTargets.Contains(request.Status))
        {
            var allowedText = allowedTargets == null || allowedTargets.Length == 0
                ? "không còn trạng thái kế tiếp"
                : string.Join(", ", allowedTargets);
            return ServiceResult<SupportRequestDto>.Fail(
                $"Không thể chuyển trạng thái từ {supportRequest.Status} sang {request.Status}. Cho phép: {allowedText}.");
        }

        supportRequest.Status = request.Status;
        supportRequest.AssignedEmployeeUserId = assignedEmployeeUserId;

        if (!string.IsNullOrWhiteSpace(request.Response))
        {
            supportRequest.Response = request.Response;
            supportRequest.RespondedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        if (supportRequest.AssignedEmployee?.Id != assignedEmployeeUserId)
        {
            supportRequest.AssignedEmployee = await _context.Users.FindAsync(assignedEmployeeUserId);
        }

        return ServiceResult<SupportRequestDto>.Success(MapToDto(supportRequest));
    }

    private static SupportRequestDto MapToDto(SupportRequest request)
    {
        return new SupportRequestDto
        {
            Id = request.Id,
            UserId = request.UserId,
            UserFullName = request.User?.FullName ?? string.Empty,
            UserEmail = request.User?.Email ?? string.Empty,
            SupportType = request.SupportType,
            Subject = request.Subject,
            Message = request.Message,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            Response = request.Response,
            RespondedAt = request.RespondedAt,
            AssignedEmployeeUserId = request.AssignedEmployeeUserId,
            AssignedEmployeeName = request.AssignedEmployee?.FullName
        };
    }
}

using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface ISupportRequestService
{
    Task<SupportRequestDto> CreateAsync(Guid userId, SupportRequestCreateRequest request);
    Task<List<SupportRequestDto>> GetRequestsAsync(Guid currentUserId, string currentUserRole);
    Task<SupportRequestDto?> GetByIdAsync(int id, Guid currentUserId, string currentUserRole);
    Task<ServiceResult<SupportRequestDto>> UpdateAsync(int id, Guid assignedEmployeeUserId, SupportRequestUpdateRequest request);
}

using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface ISpecificationService
{
    Task<SpecificationDto?> GetByProductIdAsync(int productId);
    Task<ServiceResult<SpecificationDto>> CreateAsync(int productId, SpecificationCreateRequest request);
    Task<ServiceResult<SpecificationDto>> UpdateAsync(int productId, SpecificationUpdateRequest request);
}

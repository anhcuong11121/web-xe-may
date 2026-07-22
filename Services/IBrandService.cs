using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IBrandService
{
    Task<List<BrandDto>> GetBrandsAsync();
    Task<BrandDto?> GetBrandByIdAsync(int id);
    Task<ServiceResult<BrandDto>> CreateBrandAsync(BrandCreateRequest request);
    Task<ServiceResult<BrandDto>> UpdateBrandAsync(int id, BrandUpdateRequest request);
    Task<ServiceResult<bool>> DeleteBrandAsync(int id);
}

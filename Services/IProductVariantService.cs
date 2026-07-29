using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IProductVariantService
{
    Task<List<ProductVariantDto>?> GetByProductIdAsync(int productId, bool includeInactive);
    Task<ProductVariantDto?> GetByIdAsync(int productId, int variantId, bool includeInactive);
    Task<ServiceResult<ProductVariantDto>> CreateAsync(
        int productId,
        ProductVariantCreateRequest request);
    Task<ServiceResult<ProductVariantDto>> UpdateAsync(
        int productId,
        int variantId,
        ProductVariantUpdateRequest request);
    Task<ServiceResult<ProductVariantDto>> UpdateSpecificationAsync(
        int productId,
        int variantId,
        VariantSpecificationRequest request);
    Task<ServiceResult<ProductVariantDeleteDto>> DeleteAsync(int productId, int variantId);
}

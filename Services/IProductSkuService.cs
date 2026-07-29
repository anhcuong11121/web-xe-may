using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IProductSkuService
{
    Task<List<ProductSkuDto>?> GetByVariantAsync(
        int productId,
        int variantId,
        bool includeInactive);
    Task<ProductSkuDto?> GetByIdAsync(
        int productId,
        int variantId,
        int skuId,
        bool includeInactive);
    Task<ServiceResult<ProductSkuDto>> CreateAsync(
        int productId,
        int variantId,
        ProductSkuCreateRequest request);
    Task<ServiceResult<ProductSkuDto>> UpdateAsync(
        int productId,
        int variantId,
        int skuId,
        ProductSkuUpdateRequest request);
    Task<ServiceResult<ProductSkuDeleteDto>> DeleteAsync(
        int productId,
        int variantId,
        int skuId);
}

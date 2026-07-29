using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IProductImageService
{
    Task<List<ProductImageDto>?> GetBySkuAsync(
        int productId,
        int variantId,
        int skuId,
        bool includeInactive);
    Task<ServiceResult<ProductImageDto>> UploadAsync(
        int productId,
        int variantId,
        int skuId,
        ProductImageUploadRequest request);
    Task<ServiceResult<ProductImageDto>> UpdateAsync(
        int productId,
        int variantId,
        int skuId,
        int imageId,
        ProductImageUpdateRequest request);
    Task<ServiceResult<ProductImageDeleteDto>> DeleteAsync(
        int productId,
        int variantId,
        int skuId,
        int imageId);
}

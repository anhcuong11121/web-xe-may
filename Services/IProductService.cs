using Microsoft.AspNetCore.Http;
using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryParameters query);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<bool> RecordInterestAsync(int id);
    Task<ServiceResult<ProductDto>> CreateProductAsync(ProductCreateRequest request);
    Task<ServiceResult<ProductDto>> UpdateProductAsync(int id, ProductUpdateRequest request);
    Task<ServiceResult<bool>> DeleteProductAsync(int id);
    Task<ServiceResult<ProductDto>> UploadProductImageAsync(int id, IFormFile file);
}

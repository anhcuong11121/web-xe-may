using MotorBikeShop.API.DTOs;
using Microsoft.AspNetCore.Http;

namespace MotorBikeShop.API.Services;

public interface INewsService
{
    Task<List<NewsDto>> GetAllAsync(bool includeUnpublished = false);
    Task<NewsDto?> GetByIdAsync(int id, bool includeUnpublished = false);
    Task<ServiceResult<NewsDto>> CreateAsync(Guid authorId, NewsCreateRequest request);
    Task<ServiceResult<NewsDto>> UpdateAsync(int id, NewsUpdateRequest request);
    Task<ServiceResult<bool>> DeleteAsync(int id);
    Task<ServiceResult<NewsDto>> UploadImageAsync(int id, IFormFile file);
}

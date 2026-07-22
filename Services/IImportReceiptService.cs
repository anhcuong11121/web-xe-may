using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IImportReceiptService
{
    Task<List<ImportReceiptDto>> GetAllAsync();
    Task<ImportReceiptDto?> GetByIdAsync(int id);
    Task<ServiceResult<ImportReceiptDto>> CreateAsync(Guid createdByUserId, ImportReceiptCreateRequest request);
    Task<ServiceResult<ImportReceiptDto>> CancelAsync(int id);
}

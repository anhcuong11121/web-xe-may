using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface ISupplierService
{
    Task<List<SupplierDto>> GetSuppliersAsync();
    Task<SupplierDto?> GetSupplierByIdAsync(int id);
    Task<ServiceResult<SupplierDto>> CreateSupplierAsync(SupplierCreateRequest request);
    Task<ServiceResult<SupplierDto>> UpdateSupplierAsync(int id, SupplierUpdateRequest request);
    Task<ServiceResult<bool>> DeleteSupplierAsync(int id);
}

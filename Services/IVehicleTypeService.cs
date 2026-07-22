using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IVehicleTypeService
{
    Task<List<VehicleTypeDto>> GetAllAsync();
    Task<VehicleTypeDto?> GetByIdAsync(int id);
    Task<ServiceResult<VehicleTypeDto>> CreateAsync(VehicleTypeRequest request);
    Task<ServiceResult<VehicleTypeDto>> UpdateAsync(int id, VehicleTypeRequest request);
    Task<ServiceResult<bool>> DeleteAsync(int id);
}

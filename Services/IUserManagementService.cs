using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IUserManagementService
{
    Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<PagedResult<CustomerDto>> GetCustomersAsync(int pageNumber, int pageSize);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id);
    Task<PagedResult<EmployeeDto>> GetEmployeesAsync(int pageNumber, int pageSize);
    Task<EmployeeDto?> GetEmployeeByIdAsync(Guid id);
    Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(EmployeeCreateRequest request);
    Task<ServiceResult<EmployeeDto>> UpdateEmployeeAsync(Guid id, EmployeeUpdateRequest request);
    Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateRequest request);
    Task<ServiceResult<UserDto>> UpdateUserRoleAsync(Guid id, Guid currentAdminId, UserRoleUpdateRequest request);
    Task<ServiceResult<UserDto>> LockUserAsync(Guid id, Guid currentAdminId);
    Task<ServiceResult<UserDto>> UnlockUserAsync(Guid id);
}

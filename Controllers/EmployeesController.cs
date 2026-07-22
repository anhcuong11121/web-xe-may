using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "Admin")]
public class EmployeesController : ControllerBase
{
    private readonly IUserManagementService _service;

    public EmployeesController(IUserManagementService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<EmployeeDto>>> GetEmployees(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _service.GetEmployeesAsync(pageNumber, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> GetEmployee(Guid id)
    {
        var employee = await _service.GetEmployeeByIdAsync(id);
        return employee == null ? NotFound(new { error = "Không tìm thấy nhân viên." }) : Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee(EmployeeCreateRequest request)
    {
        var result = await _service.CreateEmployeeAsync(request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetEmployee), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> UpdateEmployee(Guid id, EmployeeUpdateRequest request)
    {
        var result = await _service.UpdateEmployeeAsync(id, request);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<UserDto>> DeactivateEmployee(Guid id)
    {
        var employee = await _service.GetEmployeeByIdAsync(id);
        if (employee == null) return NotFound(new { error = "Không tìm thấy nhân viên." });
        var result = await _service.LockUserAsync(id, this.GetUserId());
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<UserDto>> ActivateEmployee(Guid id)
    {
        var employee = await _service.GetEmployeeByIdAsync(id);
        if (employee == null) return NotFound(new { error = "Không tìm thấy nhân viên." });
        var result = await _service.UnlockUserAsync(id);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}

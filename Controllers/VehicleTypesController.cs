using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/vehicle-types")]
public class VehicleTypesController : ControllerBase
{
    private readonly IVehicleTypeService _service;

    public VehicleTypesController(IVehicleTypeService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<VehicleTypeDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehicleTypeDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null ? NotFound(new { error = "Không tìm thấy loại xe." }) : Ok(item);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    public async Task<ActionResult<VehicleTypeDto>> Create(VehicleTypeRequest request)
    {
        var result = await _service.CreateAsync(request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<VehicleTypeDto>> Update(int id, VehicleTypeRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (result.Succeeded) return Ok(result.Data);
        return result.Error == "Không tìm thấy loại xe."
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.Succeeded) return NoContent();
        return result.Error == "Không tìm thấy loại xe."
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }
}

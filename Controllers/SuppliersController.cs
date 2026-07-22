using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> GetSuppliers()
    {
        return Ok(await _supplierService.GetSuppliersAsync());
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierDto>> GetSupplierById(int id)
    {
        var supplier = await _supplierService.GetSupplierByIdAsync(id);
        if (supplier == null)
        {
            return NotFound(new { error = "Không tìm thấy nhà cung cấp." });
        }

        return Ok(supplier);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] SupplierCreateRequest request)
    {
        var result = await _supplierService.CreateSupplierAsync(request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetSupplierById), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(int id, [FromBody] SupplierUpdateRequest request)
    {
        var result = await _supplierService.UpdateSupplierAsync(id, request);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy nhà cung cấp."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var result = await _supplierService.DeleteSupplierAsync(id);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy nhà cung cấp."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}

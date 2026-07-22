using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    /// <summary>
    /// Danh sách hãng xe. Public (dùng cho dropdown lọc sản phẩm).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BrandDto>>> GetBrands()
    {
        var brands = await _brandService.GetBrandsAsync();
        return Ok(brands);
    }

    /// <summary>
    /// Chi tiết hãng xe. Public.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BrandDto>> GetBrandById(int id)
    {
        var brand = await _brandService.GetBrandByIdAsync(id);
        if (brand == null)
        {
            return NotFound(new { error = "Không tìm thấy hãng xe." });
        }

        return Ok(brand);
    }

    /// <summary>
    /// Nhân viên/Admin thêm hãng xe mới.
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    public async Task<ActionResult<BrandDto>> CreateBrand([FromBody] BrandCreateRequest request)
    {
        var result = await _brandService.CreateBrandAsync(request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetBrandById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Nhân viên/Admin cập nhật hãng xe.
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BrandDto>> UpdateBrand(int id, [FromBody] BrandUpdateRequest request)
    {
        var result = await _brandService.UpdateBrandAsync(id, request);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy hãng xe."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Nhân viên/Admin xóa hãng xe (chỉ khi không còn sản phẩm liên quan).
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        var result = await _brandService.DeleteBrandAsync(id);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy hãng xe."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}

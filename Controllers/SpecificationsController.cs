using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/products/{productId:int}/specification")]
public class SpecificationsController : ControllerBase
{
    private readonly ISpecificationService _specificationService;

    public SpecificationsController(ISpecificationService specificationService)
    {
        _specificationService = specificationService;
    }

    /// <summary>
    /// Xem thông số kỹ thuật của sản phẩm. Public.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SpecificationDto>> GetSpecification(int productId)
    {
        var spec = await _specificationService.GetByProductIdAsync(productId);
        if (spec == null)
        {
            return NotFound(new { error = "Sản phẩm chưa có thông số kỹ thuật." });
        }

        return Ok(spec);
    }

    /// <summary>
    /// Nhân viên/Admin tạo thông số kỹ thuật mới cho sản phẩm.
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    public async Task<ActionResult<SpecificationDto>> CreateSpecification(int productId, [FromBody] SpecificationCreateRequest request)
    {
        var result = await _specificationService.CreateAsync(productId, request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Nhân viên/Admin cập nhật thông số kỹ thuật.
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpPut]
    public async Task<ActionResult<SpecificationDto>> UpdateSpecification(int productId, [FromBody] SpecificationUpdateRequest request)
    {
        var result = await _specificationService.UpdateAsync(productId, request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}

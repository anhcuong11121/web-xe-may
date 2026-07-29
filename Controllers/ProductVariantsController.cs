using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/products/{productId:int}/variants")]
public class ProductVariantsController : ControllerBase
{
    private readonly IProductVariantService _variantService;

    public ProductVariantsController(IProductVariantService variantService)
    {
        _variantService = variantService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductVariantDto>>> GetActiveVariants(int productId)
    {
        var variants = await _variantService.GetByProductIdAsync(
            productId,
            includeInactive: false);
        return variants == null
            ? NotFound(new { error = "Không tìm thấy sản phẩm." })
            : Ok(variants);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpGet("manage")]
    public async Task<ActionResult<List<ProductVariantDto>>> GetManagedVariants(int productId)
    {
        var variants = await _variantService.GetByProductIdAsync(
            productId,
            includeInactive: true);
        return variants == null
            ? NotFound(new { error = "Không tìm thấy sản phẩm." })
            : Ok(variants);
    }

    [HttpGet("{variantId:int}")]
    public async Task<ActionResult<ProductVariantDto>> GetVariant(
        int productId,
        int variantId)
    {
        var variant = await _variantService.GetByIdAsync(
            productId,
            variantId,
            includeInactive: false);
        return variant == null
            ? NotFound(new { error = "Không tìm thấy phiên bản." })
            : Ok(variant);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductVariantDto>> CreateVariant(
        int productId,
        [FromBody] ProductVariantCreateRequest request)
    {
        var result = await _variantService.CreateAsync(productId, request);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy sản phẩm."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetVariant),
            new { productId, variantId = result.Data!.Id },
            result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{variantId:int}")]
    public async Task<ActionResult<ProductVariantDto>> UpdateVariant(
        int productId,
        int variantId,
        [FromBody] ProductVariantUpdateRequest request)
    {
        var result = await _variantService.UpdateAsync(productId, variantId, request);
        return ToVariantActionResult(result);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{variantId:int}/specification")]
    public async Task<ActionResult<ProductVariantDto>> UpdateSpecification(
        int productId,
        int variantId,
        [FromBody] VariantSpecificationRequest request)
    {
        var result = await _variantService.UpdateSpecificationAsync(
            productId,
            variantId,
            request);
        return ToVariantActionResult(result);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpDelete("{variantId:int}")]
    public async Task<ActionResult<ProductVariantDeleteDto>> DeleteVariant(
        int productId,
        int variantId)
    {
        var result = await _variantService.DeleteAsync(productId, variantId);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy phiên bản."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    private ActionResult<ProductVariantDto> ToVariantActionResult(
        ServiceResult<ProductVariantDto> result)
    {
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy phiên bản."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}

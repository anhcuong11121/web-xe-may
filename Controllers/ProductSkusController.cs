using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/products/{productId:int}/variants/{variantId:int}/skus")]
public class ProductSkusController : ControllerBase
{
    private readonly IProductSkuService _skuService;

    public ProductSkusController(IProductSkuService skuService)
    {
        _skuService = skuService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductSkuDto>>> GetActiveSkus(
        int productId,
        int variantId)
    {
        var skus = await _skuService.GetByVariantAsync(
            productId,
            variantId,
            includeInactive: false);
        return skus == null
            ? NotFound(new { error = "Không tìm thấy phiên bản." })
            : Ok(skus);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpGet("manage")]
    public async Task<ActionResult<List<ProductSkuDto>>> GetManagedSkus(
        int productId,
        int variantId)
    {
        var skus = await _skuService.GetByVariantAsync(
            productId,
            variantId,
            includeInactive: true);
        return skus == null
            ? NotFound(new { error = "Không tìm thấy phiên bản." })
            : Ok(skus);
    }

    [HttpGet("{skuId:int}")]
    public async Task<ActionResult<ProductSkuDto>> GetSku(
        int productId,
        int variantId,
        int skuId)
    {
        var sku = await _skuService.GetByIdAsync(
            productId,
            variantId,
            skuId,
            includeInactive: false);
        return sku == null
            ? NotFound(new { error = "Không tìm thấy SKU." })
            : Ok(sku);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductSkuDto>> CreateSku(
        int productId,
        int variantId,
        [FromBody] ProductSkuCreateRequest request)
    {
        var result = await _skuService.CreateAsync(productId, variantId, request);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy phiên bản."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetSku),
            new { productId, variantId, skuId = result.Data!.Id },
            result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{skuId:int}")]
    public async Task<ActionResult<ProductSkuDto>> UpdateSku(
        int productId,
        int variantId,
        int skuId,
        [FromBody] ProductSkuUpdateRequest request)
    {
        var result = await _skuService.UpdateAsync(productId, variantId, skuId, request);
        if (!result.Succeeded)
        {
            if (result.Error == "Không tìm thấy SKU.")
            {
                return NotFound(new { error = result.Error });
            }

            return result.Error != null &&
                   result.Error.StartsWith("SKU đã được cập nhật", StringComparison.Ordinal)
                ? Conflict(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpDelete("{skuId:int}")]
    public async Task<ActionResult<ProductSkuDeleteDto>> DeleteSku(
        int productId,
        int variantId,
        int skuId)
    {
        var result = await _skuService.DeleteAsync(productId, variantId, skuId);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy SKU."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}

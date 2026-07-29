using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/products/{productId:int}/variants/{variantId:int}/skus/{skuId:int}/images")]
public class ProductImagesController : ControllerBase
{
    private readonly IProductImageService _imageService;

    public ProductImagesController(IProductImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductImageDto>>> GetImages(
        int productId,
        int variantId,
        int skuId)
    {
        var images = await _imageService.GetBySkuAsync(
            productId,
            variantId,
            skuId,
            includeInactive: false);
        return images == null
            ? NotFound(new { error = "Không tìm thấy SKU." })
            : Ok(images);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpGet("manage")]
    public async Task<ActionResult<List<ProductImageDto>>> GetManagedImages(
        int productId,
        int variantId,
        int skuId)
    {
        var images = await _imageService.GetBySkuAsync(
            productId,
            variantId,
            skuId,
            includeInactive: true);
        return images == null
            ? NotFound(new { error = "Không tìm thấy SKU." })
            : Ok(images);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProductImageDto>> UploadImage(
        int productId,
        int variantId,
        int skuId,
        [FromForm] ProductImageUploadRequest request)
    {
        var result = await _imageService.UploadAsync(
            productId,
            variantId,
            skuId,
            request);
        if (!result.Succeeded)
        {
            if (result.Error == "Không tìm thấy SKU.")
            {
                return NotFound(new { error = result.Error });
            }

            return result.Error != null &&
                   result.Error.Contains("cập nhật đồng thời", StringComparison.Ordinal)
                ? Conflict(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetImages),
            new { productId, variantId, skuId },
            result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{imageId:int}")]
    public async Task<ActionResult<ProductImageDto>> UpdateImage(
        int productId,
        int variantId,
        int skuId,
        int imageId,
        [FromBody] ProductImageUpdateRequest request)
    {
        var result = await _imageService.UpdateAsync(
            productId,
            variantId,
            skuId,
            imageId,
            request);
        if (!result.Succeeded)
        {
            if (result.Error is "Không tìm thấy SKU." or "Không tìm thấy ảnh.")
            {
                return NotFound(new { error = result.Error });
            }

            return result.Error != null &&
                   result.Error.Contains("yêu cầu đồng thời", StringComparison.Ordinal)
                ? Conflict(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpDelete("{imageId:int}")]
    public async Task<ActionResult<ProductImageDeleteDto>> DeleteImage(
        int productId,
        int variantId,
        int skuId,
        int imageId)
    {
        var result = await _imageService.DeleteAsync(
            productId,
            variantId,
            skuId,
            imageId);
        if (!result.Succeeded)
        {
            return result.Error is "Không tìm thấy SKU." or "Không tìm thấy ảnh."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Xem danh sách xe (UC03). Public, hỗ trợ lọc theo brandId/giá + phân trang.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts([FromQuery] ProductQueryParameters query)
    {
        var result = await _productService.GetProductsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Tìm kiếm / lọc xe (UC05, UC06). Public.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<ProductDto>>> SearchProducts([FromQuery] ProductQueryParameters query)
    {
        var result = await _productService.GetProductsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Danh sách catalog theo mô hình Product - Variant - SKU.
    /// Endpoint mới được thêm song song để giữ tương thích API legacy.
    /// </summary>
    [HttpGet("catalog")]
    public async Task<ActionResult<PagedResult<ProductCatalogSummaryDto>>> GetProductCatalog(
        [FromQuery] ProductQueryParameters query)
    {
        var result = await _productService.GetCatalogProductsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết Product cùng các Variant, Specification, SKU và ảnh đang hoạt động.
    /// </summary>
    [HttpGet("{id:int}/catalog")]
    public async Task<ActionResult<ProductCatalogDetailDto>> GetProductCatalogById(int id)
    {
        var product = await _productService.GetProductCatalogByIdAsync(id);
        return product == null
            ? NotFound(new { error = "Không tìm thấy sản phẩm." })
            : Ok(product);
    }

    /// <summary>
    /// Xem chi tiết sản phẩm (UC04). Public.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProductById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { error = "Không tìm thấy sản phẩm." });
        }

        return Ok(product);
    }

    [HttpPost("{id:int}/interest")]
    public async Task<IActionResult> RecordInterest(int id)
    {
        return await _productService.RecordInterestAsync(id)
            ? NoContent()
            : NotFound(new { error = "Không tìm thấy sản phẩm." });
    }

    /// <summary>
    /// Nhân viên/Admin thêm sản phẩm mới (UC12).
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] ProductCreateRequest request)
    {
        var result = await _productService.CreateProductAsync(request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetProductById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Nhân viên/Admin cập nhật sản phẩm (UC12).
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] ProductUpdateRequest request)
    {
        var result = await _productService.UpdateProductAsync(id, request);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy sản phẩm."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Nhân viên/Admin xóa sản phẩm (UC12).
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var result = await _productService.DeleteProductAsync(id);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy sản phẩm."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/imports")]
[Authorize(Roles = "Employee,Admin")]
public class ImportsController : ControllerBase
{
    private readonly IImportReceiptService _importReceiptService;

    public ImportsController(IImportReceiptService importReceiptService)
    {
        _importReceiptService = importReceiptService;
    }

    /// <summary>
    /// Tạo phiếu nhập hàng, tự động cộng tồn kho cho từng SKU màu.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ImportReceiptDto>> CreateImportReceipt([FromBody] ImportReceiptCreateRequest request)
    {
        var result = await _importReceiptService.CreateAsync(this.GetUserId(), request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetImportReceiptById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet]
    public async Task<ActionResult<List<ImportReceiptDto>>> GetImportReceipts()
    {
        return Ok(await _importReceiptService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ImportReceiptDto>> GetImportReceiptById(int id)
    {
        var receipt = await _importReceiptService.GetByIdAsync(id);
        if (receipt == null)
        {
            return NotFound(new { error = "Không tìm thấy phiếu nhập." });
        }

        return Ok(receipt);
    }

    /// <summary>
    /// Hủy phiếu nhập, giữ lịch sử chứng từ và hoàn tác tồn kho.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ImportReceiptDto>> CancelImportReceipt(int id)
    {
        var result = await _importReceiptService.CancelAsync(id);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy phiếu nhập."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}

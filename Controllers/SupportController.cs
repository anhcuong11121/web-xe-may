using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/support")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly ISupportRequestService _supportRequestService;

    public SupportController(ISupportRequestService supportRequestService)
    {
        _supportRequestService = supportRequestService;
    }

    /// <summary>
    /// Khách hàng gửi yêu cầu chăm sóc khách hàng (UC08).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SupportRequestDto>> CreateSupportRequest([FromBody] SupportRequestCreateRequest request)
    {
        var result = await _supportRequestService.CreateAsync(this.GetUserId(), request);
        return CreatedAtAction(nameof(GetSupportRequestById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Danh sách yêu cầu: Customer thấy yêu cầu của mình, Employee/Admin thấy tất cả.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SupportRequestDto>>> GetSupportRequests()
    {
        var requests = await _supportRequestService.GetRequestsAsync(this.GetUserId(), this.GetUserRole());
        return Ok(requests);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupportRequestDto>> GetSupportRequestById(int id)
    {
        var request = await _supportRequestService.GetByIdAsync(id, this.GetUserId(), this.GetUserRole());
        if (request == null)
        {
            return NotFound(new { error = "Không tìm thấy yêu cầu chăm sóc khách hàng." });
        }

        return Ok(request);
    }

    /// <summary>
    /// Nhân viên tiếp nhận và phản hồi yêu cầu (UC14).
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupportRequestDto>> UpdateSupportRequest(int id, [FromBody] SupportRequestUpdateRequest request)
    {
        var result = await _supportRequestService.UpdateAsync(id, this.GetUserId(), request);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy yêu cầu chăm sóc khách hàng."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}

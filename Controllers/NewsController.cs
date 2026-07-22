using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/news")]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;

    public NewsController(INewsService newsService)
    {
        _newsService = newsService;
    }

    /// <summary>
    /// Xem tin tức/khuyến mãi. Public.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NewsDto>>> GetAll()
    {
        return Ok(await _newsService.GetAllAsync());
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpGet("manage")]
    public async Task<ActionResult<List<NewsDto>>> GetAllForManagement()
    {
        return Ok(await _newsService.GetAllAsync(includeUnpublished: true));
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpGet("manage/{id:int}")]
    public async Task<ActionResult<NewsDto>> GetByIdForManagement(int id)
    {
        var news = await _newsService.GetByIdAsync(id, includeUnpublished: true);
        return news == null ? NotFound(new { error = "Không tìm thấy tin tức." }) : Ok(news);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NewsDto>> GetById(int id)
    {
        var news = await _newsService.GetByIdAsync(id);
        if (news == null)
        {
            return NotFound(new { error = "Không tìm thấy tin tức." });
        }

        return Ok(news);
    }

    /// <summary>
    /// Nhân viên/Admin quản lý tin tức và khuyến mãi (UC17).
    /// </summary>
    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    public async Task<ActionResult<NewsDto>> Create([FromBody] NewsCreateRequest request)
    {
        var result = await _newsService.CreateAsync(this.GetUserId(), request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetByIdForManagement), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<NewsDto>> Update(int id, [FromBody] NewsUpdateRequest request)
    {
        var result = await _newsService.UpdateAsync(id, request);
        if (!result.Succeeded)
        {
            return result.Error == "Không tìm thấy tin tức."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _newsService.DeleteAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPost("{id:int}/image")]
    public async Task<ActionResult<NewsDto>> UploadImage(int id, IFormFile file)
    {
        var result = await _newsService.UploadImageAsync(id, file);
        if (result.Succeeded) return Ok(result.Data);
        return result.Error == "Không tìm thấy tin tức."
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }
}

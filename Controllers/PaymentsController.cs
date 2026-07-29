using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [AllowAnonymous]
    [HttpGet("configuration")]
    public ActionResult<PaymentConfigurationDto> GetConfiguration()
    {
        return Ok(new PaymentConfigurationDto
        {
            Mode = "Demo",
            HasRealPaymentGateway = false,
            Notice = string.Empty,
            Methods = new List<PaymentMethodConfigurationDto>
            {
                new() { Code = PaymentMethods.Demo, Name = "Xác nhận thanh toán", ConfirmationType = "Simulated" },
                new() { Code = PaymentMethods.BankTransfer, Name = "Chuyển khoản", ConfirmationType = "ManualConfirmation" },
                new() { Code = PaymentMethods.Cash, Name = "Tiền mặt", ConfirmationType = "ManualConfirmation" }
            }
        });
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("initiate")]
    public async Task<ActionResult<PaymentAttemptDto>> Initiate(PaymentInitiateRequest request)
    {
        var result = await _paymentService.InitiateAsync(this.GetUserId(), request);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<PaymentConfirmationDto>> ConfirmFake(Guid id)
    {
        var result = await _paymentService.ConfirmFakeAsync(id, this.GetUserId());
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("{id:guid}/fail")]
    public async Task<ActionResult<PaymentAttemptDto>> FailFake(Guid id)
    {
        var result = await _paymentService.FailFakeAsync(id, this.GetUserId());
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPost("{id:guid}/complete-manual")]
    public async Task<ActionResult<PaymentConfirmationDto>> CompleteManual(Guid id)
    {
        var result = await _paymentService.CompleteManualAsync(id, this.GetUserId());
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentAttemptDto>>> GetList(
        [FromQuery] PaymentAttemptQueryParameters query)
    {
        var result = await _paymentService.GetListAsync(
            query,
            this.GetUserId(),
            this.GetUserRole());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentAttemptDto>> GetById(Guid id)
    {
        var attempt = await _paymentService.GetByIdAsync(id, this.GetUserId(), this.GetUserRole());
        return attempt == null ? NotFound(new { error = "Không tìm thấy phiên thanh toán." }) : Ok(attempt);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Services;

namespace MotorBikeShop.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Employee,Admin")]
public class CustomersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public CustomersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await _userManagementService.GetCustomersAsync(pageNumber, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetCustomerById(Guid id)
    {
        var customer = await _userManagementService.GetCustomerByIdAsync(id);
        return customer == null
            ? NotFound(new { error = "Không tìm thấy khách hàng." })
            : Ok(customer);
    }
}

using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class EmployeeCreateRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, Phone, MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string Password { get; set; } = string.Empty;
}

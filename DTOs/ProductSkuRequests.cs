using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class ProductSkuCreateRequest
{
    [Required]
    [MaxLength(64)]
    public string SkuCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ColorName { get; set; } = string.Empty;

    [MaxLength(9)]
    public string? ColorHexCode { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";
}

public class ProductSkuUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string ColorName { get; set; } = string.Empty;

    [MaxLength(9)]
    public string? ColorHexCode { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public class ProductSkuDeleteDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Status { get; set; }
}

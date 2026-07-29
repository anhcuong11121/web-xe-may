using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MotorBikeShop.API.DTOs;

public class ProductImageUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [MaxLength(200)]
    public string? AltText { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }
}

public class ProductImageUpdateRequest
{
    [MaxLength(200)]
    public string? AltText { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }
}

public class ProductImageDeleteDto
{
    public int Id { get; set; }
    public int ProductSkuId { get; set; }
    public int? PromotedImageId { get; set; }
}

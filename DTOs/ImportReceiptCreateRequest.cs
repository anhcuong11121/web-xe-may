using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class ImportReceiptDetailCreateRequest
{
    [Required]
    public int ProductSkuId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }
}

public class ImportReceiptCreateRequest
{
    [MaxLength(100)]
    public string? ReceiptNumber { get; set; }

    [Required]
    public int SupplierId { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Phiếu nhập phải có ít nhất 1 sản phẩm.")]
    public List<ImportReceiptDetailCreateRequest> Details { get; set; } = new();
}

using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class OrderItemCreateRequest
{
    [Range(1, int.MaxValue)]
    public int ProductSkuId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class OrderCreateRequest
{
    [Required]
    [MaxLength(100)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Note { get; set; }

    [Required]
    public DateTime ExpectedDeliveryDate { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất 1 sản phẩm.")]
    public List<OrderItemCreateRequest> Items { get; set; } = new();
}

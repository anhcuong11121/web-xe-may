using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class ImportReceiptDetail
{
    public int ImportReceiptId { get; set; }

    public int ProductSkuId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }

    public virtual ImportReceipt ImportReceipt { get; set; } = null!;

    public virtual ProductSku ProductSku { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class ImportReceipt
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string ReceiptNumber { get; set; } = string.Empty;

    public DateTime ImportDate { get; set; } = DateTime.UtcNow;

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    public int SupplierId { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
}

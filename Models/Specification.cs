using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class Specification
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string EngineType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FuelType { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int EngineCapacityCc { get; set; }

    [Range(0, int.MaxValue)]
    public int HorsePower { get; set; }

    public int ProductId { get; set; }

    public virtual Product Product { get; set; } = null!;
}

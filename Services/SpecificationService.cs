using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class SpecificationService : ISpecificationService
{
    private readonly ApplicationDbContext _context;

    public SpecificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SpecificationDto?> GetByProductIdAsync(int productId)
    {
        var spec = await _context.Specifications.FirstOrDefaultAsync(s => s.ProductId == productId);
        return spec == null ? null : MapToDto(spec);
    }

    public async Task<ServiceResult<SpecificationDto>> CreateAsync(int productId, SpecificationCreateRequest request)
    {
        var product = await _context.Products
            .Include(p => p.Specification)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return ServiceResult<SpecificationDto>.Fail("Không tìm thấy sản phẩm.");
        }

        if (product.Specification != null)
        {
            return ServiceResult<SpecificationDto>.Fail("Sản phẩm đã có thông số kỹ thuật. Vui lòng dùng PUT để cập nhật.");
        }

        var spec = new Specification
        {
            ProductId = productId,
            EngineType = request.EngineType,
            FuelType = request.FuelType,
            EngineCapacityCc = request.EngineCapacityCc,
            HorsePower = request.HorsePower,
            CurbWeightKg = request.CurbWeightKg,
            Dimensions = request.Dimensions,
            FuelTankCapacityLiters = request.FuelTankCapacityLiters,
            MaxPower = request.MaxPower,
            FuelConsumptionLitersPer100Km = request.FuelConsumptionLitersPer100Km,
            OtherDetails = request.OtherDetails
        };

        _context.Specifications.Add(spec);
        await _context.SaveChangesAsync();

        return ServiceResult<SpecificationDto>.Success(MapToDto(spec));
    }

    public async Task<ServiceResult<SpecificationDto>> UpdateAsync(int productId, SpecificationUpdateRequest request)
    {
        var spec = await _context.Specifications.FirstOrDefaultAsync(s => s.ProductId == productId);
        if (spec == null)
        {
            return ServiceResult<SpecificationDto>.Fail("Sản phẩm chưa có thông số kỹ thuật. Vui lòng dùng POST để tạo mới.");
        }

        spec.EngineType = request.EngineType;
        spec.FuelType = request.FuelType;
        spec.EngineCapacityCc = request.EngineCapacityCc;
        spec.HorsePower = request.HorsePower;
        spec.CurbWeightKg = request.CurbWeightKg;
        spec.Dimensions = request.Dimensions;
        spec.FuelTankCapacityLiters = request.FuelTankCapacityLiters;
        spec.MaxPower = request.MaxPower;
        spec.FuelConsumptionLitersPer100Km = request.FuelConsumptionLitersPer100Km;
        spec.OtherDetails = request.OtherDetails;

        await _context.SaveChangesAsync();

        return ServiceResult<SpecificationDto>.Success(MapToDto(spec));
    }

    private static SpecificationDto MapToDto(Specification spec)
    {
        return new SpecificationDto
        {
            EngineType = spec.EngineType,
            FuelType = spec.FuelType,
            EngineCapacityCc = spec.EngineCapacityCc,
            HorsePower = spec.HorsePower,
            CurbWeightKg = spec.CurbWeightKg,
            Dimensions = spec.Dimensions,
            FuelTankCapacityLiters = spec.FuelTankCapacityLiters,
            MaxPower = spec.MaxPower,
            FuelConsumptionLitersPer100Km = spec.FuelConsumptionLitersPer100Km,
            OtherDetails = spec.OtherDetails
        };
    }
}

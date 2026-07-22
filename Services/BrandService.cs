using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class BrandService : IBrandService
{
    private readonly ApplicationDbContext _context;

    public BrandService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrandDto>> GetBrandsAsync()
    {
        var brands = await _context.Brands
            .Include(b => b.Products)
            .OrderBy(b => b.Name)
            .ToListAsync();

        return brands.Select(MapToDto).ToList();
    }

    public async Task<BrandDto?> GetBrandByIdAsync(int id)
    {
        var brand = await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Id == id);

        return brand == null ? null : MapToDto(brand);
    }

    public async Task<ServiceResult<BrandDto>> CreateBrandAsync(BrandCreateRequest request)
    {
        var normalizedName = request.Name.Trim();
        if (normalizedName.Length == 0)
        {
            return ServiceResult<BrandDto>.Fail("Tên hãng xe không được để trống.");
        }

        var nameKey = normalizedName.ToUpper();
        var nameExists = await _context.Brands
            .AnyAsync(brand => brand.Name.ToUpper() == nameKey);
        if (nameExists)
        {
            return ServiceResult<BrandDto>.Fail("Tên hãng xe đã tồn tại.");
        }

        var brand = new Brand
        {
            Name = normalizedName,
            Description = request.Description?.Trim(),
            Country = request.Country?.Trim(),
            LogoUrl = request.LogoUrl?.Trim()
        };

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        return ServiceResult<BrandDto>.Success(MapToDto(brand));
    }

    public async Task<ServiceResult<BrandDto>> UpdateBrandAsync(int id, BrandUpdateRequest request)
    {
        var normalizedName = request.Name.Trim();
        if (normalizedName.Length == 0)
        {
            return ServiceResult<BrandDto>.Fail("Tên hãng xe không được để trống.");
        }

        var brand = await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (brand == null)
        {
            return ServiceResult<BrandDto>.Fail("Không tìm thấy hãng xe.");
        }

        var nameKey = normalizedName.ToUpper();
        var nameExists = await _context.Brands
            .AnyAsync(candidate => candidate.Id != id && candidate.Name.ToUpper() == nameKey);
        if (nameExists)
        {
            return ServiceResult<BrandDto>.Fail("Tên hãng xe đã tồn tại.");
        }

        brand.Name = normalizedName;
        brand.Description = request.Description?.Trim();
        brand.Country = request.Country?.Trim();
        brand.LogoUrl = request.LogoUrl?.Trim();

        await _context.SaveChangesAsync();

        return ServiceResult<BrandDto>.Success(MapToDto(brand));
    }

    public async Task<ServiceResult<bool>> DeleteBrandAsync(int id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand == null)
        {
            return ServiceResult<bool>.Fail("Không tìm thấy hãng xe.");
        }

        var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
        if (hasProducts)
        {
            return ServiceResult<bool>.Fail("Không thể xóa vì hãng xe đang có sản phẩm liên quan.");
        }

        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    private static BrandDto MapToDto(Brand brand)
    {
        return new BrandDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Description = brand.Description,
            Country = brand.Country,
            LogoUrl = brand.LogoUrl,
            ProductCount = brand.Products.Count
        };
    }
}

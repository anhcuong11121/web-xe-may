using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class VehicleTypeService : IVehicleTypeService
{
    private readonly ApplicationDbContext _context;

    public VehicleTypeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VehicleTypeDto>> GetAllAsync()
    {
        return await _context.VehicleTypes
            .OrderBy(v => v.Name)
            .Select(v => new VehicleTypeDto
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                ProductCount = v.Products.Count
            })
            .ToListAsync();
    }

    public async Task<VehicleTypeDto?> GetByIdAsync(int id)
    {
        return await _context.VehicleTypes
            .Where(v => v.Id == id)
            .Select(v => new VehicleTypeDto
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                ProductCount = v.Products.Count
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<VehicleTypeDto>> CreateAsync(VehicleTypeRequest request)
    {
        var name = request.Name.Trim();
        if (await _context.VehicleTypes.AnyAsync(v => v.Name == name))
        {
            return ServiceResult<VehicleTypeDto>.Fail("Tên loại xe đã tồn tại.");
        }

        var entity = new VehicleType { Name = name, Description = request.Description };
        _context.VehicleTypes.Add(entity);
        await _context.SaveChangesAsync();

        return ServiceResult<VehicleTypeDto>.Success(Map(entity));
    }

    public async Task<ServiceResult<VehicleTypeDto>> UpdateAsync(int id, VehicleTypeRequest request)
    {
        var entity = await _context.VehicleTypes.FindAsync(id);
        if (entity == null)
        {
            return ServiceResult<VehicleTypeDto>.Fail("Không tìm thấy loại xe.");
        }

        var name = request.Name.Trim();
        if (await _context.VehicleTypes.AnyAsync(v => v.Id != id && v.Name == name))
        {
            return ServiceResult<VehicleTypeDto>.Fail("Tên loại xe đã tồn tại.");
        }

        entity.Name = name;
        entity.Description = request.Description;
        await _context.SaveChangesAsync();

        entity.Products = await _context.Products.Where(p => p.VehicleTypeId == id).ToListAsync();
        return ServiceResult<VehicleTypeDto>.Success(Map(entity));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        var entity = await _context.VehicleTypes.FindAsync(id);
        if (entity == null)
        {
            return ServiceResult<bool>.Fail("Không tìm thấy loại xe.");
        }

        if (await _context.Products.AnyAsync(p => p.VehicleTypeId == id))
        {
            return ServiceResult<bool>.Fail("Không thể xóa vì loại xe đang có sản phẩm liên quan.");
        }

        _context.VehicleTypes.Remove(entity);
        await _context.SaveChangesAsync();
        return ServiceResult<bool>.Success(true);
    }

    private static VehicleTypeDto Map(VehicleType entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        ProductCount = entity.Products.Count
    };
}

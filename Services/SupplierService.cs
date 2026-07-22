using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class SupplierService : ISupplierService
{
    private static readonly string[] AllowedStatuses = { "Active", "Inactive" };

    private readonly ApplicationDbContext _context;

    public SupplierService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SupplierDto>> GetSuppliersAsync()
    {
        var suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
        return suppliers.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<ServiceResult<SupplierDto>> CreateSupplierAsync(SupplierCreateRequest request)
    {
        if (!AllowedStatuses.Contains(request.Status))
        {
            return ServiceResult<SupplierDto>.Fail("Status không hợp lệ. Cho phép: Active, Inactive.");
        }

        var normalizedEmail = request.Email.Trim();
        var emailKey = normalizedEmail.ToUpper();
        if (await _context.Suppliers.AnyAsync(supplier => supplier.Email.ToUpper() == emailKey))
        {
            return ServiceResult<SupplierDto>.Fail("Email nhà cung cấp đã tồn tại.");
        }

        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson.Trim(),
            Phone = request.Phone.Trim(),
            Email = normalizedEmail,
            Address = request.Address?.Trim(),
            Status = request.Status
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return ServiceResult<SupplierDto>.Success(MapToDto(supplier));
    }

    public async Task<ServiceResult<SupplierDto>> UpdateSupplierAsync(int id, SupplierUpdateRequest request)
    {
        if (!AllowedStatuses.Contains(request.Status))
        {
            return ServiceResult<SupplierDto>.Fail("Status không hợp lệ. Cho phép: Active, Inactive.");
        }

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (supplier == null)
        {
            return ServiceResult<SupplierDto>.Fail("Không tìm thấy nhà cung cấp.");
        }

        var normalizedEmail = request.Email.Trim();
        var emailKey = normalizedEmail.ToUpper();
        if (await _context.Suppliers.AnyAsync(candidate =>
                candidate.Id != id && candidate.Email.ToUpper() == emailKey))
        {
            return ServiceResult<SupplierDto>.Fail("Email nhà cung cấp đã tồn tại.");
        }

        supplier.Name = request.Name.Trim();
        supplier.ContactPerson = request.ContactPerson.Trim();
        supplier.Phone = request.Phone.Trim();
        supplier.Email = normalizedEmail;
        supplier.Address = request.Address?.Trim();
        supplier.Status = request.Status;

        await _context.SaveChangesAsync();

        return ServiceResult<SupplierDto>.Success(MapToDto(supplier));
    }

    public async Task<ServiceResult<bool>> DeleteSupplierAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
        {
            return ServiceResult<bool>.Fail("Không tìm thấy nhà cung cấp.");
        }

        var hasImportReceipts = await _context.ImportReceipts.AnyAsync(ir => ir.SupplierId == id);
        if (hasImportReceipts)
        {
            return ServiceResult<bool>.Fail("Không thể xóa vì nhà cung cấp đã có phiếu nhập hàng liên quan.");
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            Status = supplier.Status
        };
    }
}

using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class ImportReceiptService : IImportReceiptService
{
    private readonly ApplicationDbContext _context;

    public ImportReceiptService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ImportReceiptDto>> GetAllAsync()
    {
        var receipts = await _context.ImportReceipts
            .Include(ir => ir.Supplier)
            .Include(ir => ir.CreatedBy)
            .Include(ir => ir.ImportReceiptDetails).ThenInclude(d => d.Product)
            .OrderByDescending(ir => ir.ImportDate)
            .ToListAsync();

        return receipts.Select(MapToDto).ToList();
    }

    public async Task<ImportReceiptDto?> GetByIdAsync(int id)
    {
        var receipt = await _context.ImportReceipts
            .Include(ir => ir.Supplier)
            .Include(ir => ir.CreatedBy)
            .Include(ir => ir.ImportReceiptDetails).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(ir => ir.Id == id);

        return receipt == null ? null : MapToDto(receipt);
    }

    public async Task<ServiceResult<ImportReceiptDto>> CreateAsync(Guid createdByUserId, ImportReceiptCreateRequest request)
    {
        if (request.Details.Count == 0)
        {
            return ServiceResult<ImportReceiptDto>.Fail("Phiếu nhập phải có ít nhất 1 sản phẩm.");
        }

        if (request.Details.Any(detail =>
                detail.ProductId <= 0 || detail.Quantity <= 0 || detail.UnitCost < 0))
        {
            return ServiceResult<ImportReceiptDto>.Fail(
                "Sản phẩm, số lượng hoặc đơn giá trong phiếu nhập không hợp lệ.");
        }

        var duplicateProductIds = request.Details
            .GroupBy(d => d.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateProductIds.Count > 0)
        {
            return ServiceResult<ImportReceiptDto>.Fail(
                $"Mỗi sản phẩm chỉ được xuất hiện một lần trong phiếu nhập. ProductId bị trùng: {string.Join(", ", duplicateProductIds)}.");
        }

        var isRelationalDatabase = _context.Database.IsRelational();
        IDbContextTransaction? transaction = null;
        if (isRelationalDatabase)
        {
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        }

        await using var transactionScope = transaction;
        var requestedReceiptNumber = string.IsNullOrWhiteSpace(request.ReceiptNumber)
            ? null
            : request.ReceiptNumber.Trim();
        if (requestedReceiptNumber != null &&
            await _context.ImportReceipts.AnyAsync(receipt => receipt.ReceiptNumber == requestedReceiptNumber))
        {
            return ServiceResult<ImportReceiptDto>.Fail("Mã phiếu nhập đã tồn tại.");
        }

        var supplierIsActive = await _context.Suppliers
            .AnyAsync(s => s.Id == request.SupplierId && s.Status == "Active");
        if (!supplierIsActive)
        {
            return ServiceResult<ImportReceiptDto>.Fail("Nhà cung cấp không tồn tại hoặc đã ngừng hợp tác.");
        }

        var productIds = request.Details.Select(d => d.ProductId).Distinct().ToList();
        var productQuery = _context.Products.Where(p => productIds.Contains(p.Id));
        var products = isRelationalDatabase
            ? await productQuery.AsNoTracking().ToListAsync()
            : await productQuery.ToListAsync();

        if (products.Count != productIds.Count)
        {
            return ServiceResult<ImportReceiptDto>.Fail("Có sản phẩm không tồn tại trong phiếu nhập.");
        }

        var receipt = new ImportReceipt
        {
            ReceiptNumber = string.IsNullOrWhiteSpace(request.ReceiptNumber)
                ? $"PN{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..38]
                : requestedReceiptNumber!,
            ImportDate = DateTime.UtcNow,
            SupplierId = request.SupplierId,
            CreatedByUserId = createdByUserId,
            Note = request.Note,
            Status = "Completed"
        };

        decimal totalAmount = 0m;

        foreach (var detail in request.Details)
        {
            var product = products.First(p => p.Id == detail.ProductId);

            receipt.ImportReceiptDetails.Add(new ImportReceiptDetail
            {
                ProductId = detail.ProductId,
                Quantity = detail.Quantity,
                UnitCost = detail.UnitCost
            });

            totalAmount += detail.Quantity * detail.UnitCost;
            if (!isRelationalDatabase)
            {
                product.StockQuantity += detail.Quantity;
            }
        }

        receipt.TotalAmount = totalAmount;

        if (isRelationalDatabase)
        {
            foreach (var detail in request.Details)
            {
                var maximumCurrentStock = int.MaxValue - detail.Quantity;
                var updated = await _context.Products
                    .Where(product =>
                        product.Id == detail.ProductId &&
                        product.StockQuantity <= maximumCurrentStock)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        product => product.StockQuantity,
                        product => product.StockQuantity + detail.Quantity));
                if (updated == 0)
                {
                    await transaction!.RollbackAsync();
                    return ServiceResult<ImportReceiptDto>.Fail(
                        "Không thể cập nhật tồn kho vì số lượng vượt giới hạn cho phép.");
                }
            }
        }

        _context.ImportReceipts.Add(receipt);
        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }

        await _context.Entry(receipt).Reference(r => r.Supplier).LoadAsync();
        await _context.Entry(receipt).Reference(r => r.CreatedBy).LoadAsync();
        foreach (var detail in receipt.ImportReceiptDetails)
        {
            await _context.Entry(detail).Reference(d => d.Product).LoadAsync();
        }

        return ServiceResult<ImportReceiptDto>.Success(MapToDto(receipt));
    }

    public async Task<ServiceResult<ImportReceiptDto>> CancelAsync(int id)
    {
        var isRelationalDatabase = _context.Database.IsRelational();
        IDbContextTransaction? transaction = null;
        if (isRelationalDatabase)
        {
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        }

        await using var transactionScope = transaction;
        var receipt = await _context.ImportReceipts
            .Include(item => item.Supplier)
            .Include(item => item.CreatedBy)
            .Include(item => item.ImportReceiptDetails).ThenInclude(detail => detail.Product)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (receipt == null)
        {
            return ServiceResult<ImportReceiptDto>.Fail("Không tìm thấy phiếu nhập.");
        }

        if (receipt.Status == "Cancelled")
        {
            if (transaction != null) await transaction.CommitAsync();
            return ServiceResult<ImportReceiptDto>.Success(MapToDto(receipt));
        }

        if (receipt.Status != "Completed")
        {
            return ServiceResult<ImportReceiptDto>.Fail(
                $"Không thể hủy phiếu nhập ở trạng thái {receipt.Status}.");
        }

        foreach (var detail in receipt.ImportReceiptDetails)
        {
            if (isRelationalDatabase)
            {
                var updated = await _context.Products
                    .Where(product => product.Id == detail.ProductId && product.StockQuantity >= detail.Quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        product => product.StockQuantity,
                        product => product.StockQuantity - detail.Quantity));
                if (updated == 0)
                {
                    await transaction!.RollbackAsync();
                    return ServiceResult<ImportReceiptDto>.Fail(
                        $"Không thể hủy vì tồn kho sản phẩm '{detail.Product.Name}' không đủ để hoàn tác phiếu nhập.");
                }
            }
            else
            {
                if (detail.Product.StockQuantity < detail.Quantity)
                {
                    return ServiceResult<ImportReceiptDto>.Fail(
                        $"Không thể hủy vì tồn kho sản phẩm '{detail.Product.Name}' không đủ để hoàn tác phiếu nhập.");
                }
                detail.Product.StockQuantity -= detail.Quantity;
            }
        }

        receipt.Status = "Cancelled";
        await _context.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();
        return ServiceResult<ImportReceiptDto>.Success(MapToDto(receipt));
    }

    private static ImportReceiptDto MapToDto(ImportReceipt receipt)
    {
        return new ImportReceiptDto
        {
            Id = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            ImportDate = receipt.ImportDate,
            TotalAmount = receipt.TotalAmount,
            Note = receipt.Note,
            Status = receipt.Status,
            SupplierId = receipt.SupplierId,
            SupplierName = receipt.Supplier?.Name ?? string.Empty,
            CreatedByUserId = receipt.CreatedByUserId,
            CreatedByName = receipt.CreatedBy?.FullName ?? string.Empty,
            Details = receipt.ImportReceiptDetails.Select(d => new ImportReceiptDetailDto
            {
                ProductId = d.ProductId,
                ProductName = d.Product?.Name ?? string.Empty,
                Quantity = d.Quantity,
                UnitCost = d.UnitCost
            }).ToList()
        };
    }
}

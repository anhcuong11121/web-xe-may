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
            .Include(ir => ir.ImportReceiptDetails)
                .ThenInclude(d => d.ProductSku)
                    .ThenInclude(sku => sku.ProductVariant)
                        .ThenInclude(variant => variant.Product)
            .AsSplitQuery()
            .OrderByDescending(ir => ir.ImportDate)
            .ToListAsync();

        return receipts.Select(MapToDto).ToList();
    }

    public async Task<ImportReceiptDto?> GetByIdAsync(int id)
    {
        var receipt = await _context.ImportReceipts
            .Include(ir => ir.Supplier)
            .Include(ir => ir.CreatedBy)
            .Include(ir => ir.ImportReceiptDetails)
                .ThenInclude(d => d.ProductSku)
                    .ThenInclude(sku => sku.ProductVariant)
                        .ThenInclude(variant => variant.Product)
            .AsSplitQuery()
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
                detail.ProductSkuId <= 0 || detail.Quantity <= 0 || detail.UnitCost < 0))
        {
            return ServiceResult<ImportReceiptDto>.Fail(
                "SKU, số lượng hoặc đơn giá trong phiếu nhập không hợp lệ.");
        }

        var duplicateSkuIds = request.Details
            .GroupBy(d => d.ProductSkuId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateSkuIds.Count > 0)
        {
            return ServiceResult<ImportReceiptDto>.Fail(
                $"Mỗi SKU chỉ được xuất hiện một lần trong phiếu nhập. ProductSkuId bị trùng: {string.Join(", ", duplicateSkuIds)}.");
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

        var skuIds = request.Details
            .Select(detail => detail.ProductSkuId)
            .OrderBy(id => id)
            .ToList();
        var skuQuery = _context.ProductSkus
            .Include(sku => sku.ProductVariant)
                .ThenInclude(variant => variant.Product)
            .Where(sku => skuIds.Contains(sku.Id));
        var skus = isRelationalDatabase
            ? await skuQuery.AsNoTracking().ToListAsync()
            : await skuQuery.ToListAsync();

        if (skus.Count != skuIds.Count)
        {
            return ServiceResult<ImportReceiptDto>.Fail("Có SKU không tồn tại trong phiếu nhập.");
        }

        var unavailableSku = skus.FirstOrDefault(sku =>
            sku.Status != CatalogStatuses.Active ||
            sku.ProductVariant.Status != CatalogStatuses.Active);
        if (unavailableSku != null)
        {
            return ServiceResult<ImportReceiptDto>.Fail(
                $"SKU '{unavailableSku.SkuCode}' hiện không hoạt động.");
        }

        var detailBySkuId = request.Details.ToDictionary(
            detail => detail.ProductSkuId);
        foreach (var sku in skus)
        {
            var quantity = detailBySkuId[sku.Id].Quantity;
            if (sku.StockQuantity > int.MaxValue - quantity)
            {
                return ServiceResult<ImportReceiptDto>.Fail(
                    $"Tồn kho SKU '{sku.SkuCode}' sẽ vượt giới hạn cho phép.");
            }
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
            var sku = skus.First(candidate => candidate.Id == detail.ProductSkuId);
            receipt.ImportReceiptDetails.Add(new ImportReceiptDetail
            {
                ProductSkuId = sku.Id,
                Quantity = detail.Quantity,
                UnitCost = detail.UnitCost
            });

            totalAmount += detail.Quantity * detail.UnitCost;
        }

        receipt.TotalAmount = totalAmount;

        if (isRelationalDatabase)
        {
            foreach (var detail in request.Details.OrderBy(item => item.ProductSkuId))
            {
                var maximumCurrentStock = int.MaxValue - detail.Quantity;
                var updated = await _context.ProductSkus
                    .Where(sku =>
                        sku.Id == detail.ProductSkuId &&
                        sku.Status == CatalogStatuses.Active &&
                        sku.ProductVariant.Status == CatalogStatuses.Active &&
                        sku.StockQuantity <= maximumCurrentStock)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        sku => sku.StockQuantity,
                        sku => sku.StockQuantity + detail.Quantity));
                if (updated == 0)
                {
                    await transaction!.RollbackAsync();
                    return ServiceResult<ImportReceiptDto>.Fail(
                        "Không thể cập nhật tồn kho SKU vì số lượng vượt giới hạn cho phép.");
                }
            }

        }
        else
        {
            foreach (var sku in skus)
            {
                sku.StockQuantity += detailBySkuId[sku.Id].Quantity;
            }

        }

        try
        {
            _context.ImportReceipts.Add(receipt);
            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch (DbUpdateException)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            else
            {
                foreach (var sku in skus)
                {
                    sku.StockQuantity -= detailBySkuId[sku.Id].Quantity;
                }

            }

            _context.ChangeTracker.Clear();
            return ServiceResult<ImportReceiptDto>.Fail(
                "Không thể tạo phiếu nhập; toàn bộ thay đổi tồn kho đã được hoàn tác.");
        }

        var createdReceipt = await _context.ImportReceipts
            .AsNoTracking()
            .Include(item => item.Supplier)
            .Include(item => item.CreatedBy)
            .Include(item => item.ImportReceiptDetails)
                .ThenInclude(detail => detail.ProductSku)
                    .ThenInclude(sku => sku.ProductVariant)
                        .ThenInclude(variant => variant.Product)
            .AsSplitQuery()
            .SingleAsync(item => item.Id == receipt.Id);
        return ServiceResult<ImportReceiptDto>.Success(MapToDto(createdReceipt));
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
            .Include(item => item.ImportReceiptDetails)
                .ThenInclude(detail => detail.ProductSku)
                    .ThenInclude(sku => sku.ProductVariant)
                        .ThenInclude(variant => variant.Product)
            .AsSplitQuery()
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

        var skuDetails = receipt.ImportReceiptDetails
            .OrderBy(detail => detail.ProductSkuId)
            .ToList();
        if (skuDetails.Any(detail =>
                detail.ProductSku.StockQuantity < detail.Quantity))
        {
            return ServiceResult<ImportReceiptDto>.Fail(
                "Không thể hủy vì tồn kho hiện tại không đủ để hoàn tác phiếu nhập.");
        }

        if (isRelationalDatabase)
        {
            foreach (var detail in skuDetails)
            {
                var updated = await _context.ProductSkus
                    .Where(sku =>
                        sku.Id == detail.ProductSkuId &&
                        sku.StockQuantity >= detail.Quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        sku => sku.StockQuantity,
                        sku => sku.StockQuantity - detail.Quantity));
                if (updated == 0)
                {
                    await transaction!.RollbackAsync();
                    return ServiceResult<ImportReceiptDto>.Fail(
                        $"Không thể hủy vì tồn kho SKU '{detail.ProductSku.SkuCode}' không đủ để hoàn tác phiếu nhập.");
                }
            }

        }
        else
        {
            foreach (var detail in skuDetails)
            {
                detail.ProductSku.StockQuantity -= detail.Quantity;
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
                ProductSkuId = d.ProductSkuId,
                ProductName = d.ProductSku.ProductVariant.Product.Name,
                VariantName = d.ProductSku.ProductVariant.Name,
                ColorName = d.ProductSku.ColorName,
                SkuCode = d.ProductSku.SkuCode,
                Quantity = d.Quantity,
                UnitCost = d.UnitCost
            }).ToList()
        };
    }
}

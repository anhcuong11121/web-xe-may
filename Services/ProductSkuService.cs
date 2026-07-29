using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class ProductSkuService : IProductSkuService
{
    private static readonly Regex SkuCodePattern = new(
        @"^[A-Z0-9]+(?:-[A-Z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ColorHexPattern = new(
        @"^#[0-9A-F]{6}(?:[0-9A-F]{2})?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ApplicationDbContext _context;

    public ProductSkuService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductSkuDto>?> GetByVariantAsync(
        int productId,
        int variantId,
        bool includeInactive)
    {
        var variantExists = await _context.ProductVariants.AnyAsync(variant =>
            variant.Id == variantId &&
            variant.ProductId == productId &&
            (includeInactive || variant.Status == CatalogStatuses.Active));
        if (!variantExists)
        {
            return null;
        }

        var skus = await SkuQuery()
            .Where(sku =>
                sku.ProductVariantId == variantId &&
                (includeInactive || sku.Status == CatalogStatuses.Active))
            .OrderBy(sku => sku.Id)
            .ToListAsync();

        return skus.Select(ProductCatalogMapper.MapSku).ToList();
    }

    public async Task<ProductSkuDto?> GetByIdAsync(
        int productId,
        int variantId,
        int skuId,
        bool includeInactive)
    {
        var sku = await SkuQuery()
            .FirstOrDefaultAsync(item =>
                item.Id == skuId &&
                item.ProductVariantId == variantId &&
                item.ProductVariant.ProductId == productId &&
                (includeInactive ||
                 (item.Status == CatalogStatuses.Active &&
                  item.ProductVariant.Status == CatalogStatuses.Active)));

        return sku == null ? null : ProductCatalogMapper.MapSku(sku);
    }

    public async Task<ServiceResult<ProductSkuDto>> CreateAsync(
        int productId,
        int variantId,
        ProductSkuCreateRequest request)
    {
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(item => item.Id == variantId && item.ProductId == productId);
        if (variant == null)
        {
            return ServiceResult<ProductSkuDto>.Fail("Không tìm thấy phiên bản.");
        }

        var normalized = ValidateAndNormalize(
            request.SkuCode,
            request.ColorName,
            request.ColorHexCode,
            request.Price,
            request.Status);
        if (!normalized.Succeeded)
        {
            return ServiceResult<ProductSkuDto>.Fail(normalized.Error!);
        }

        if (variant.Status != CatalogStatuses.Active &&
            normalized.Status == CatalogStatuses.Active)
        {
            return ServiceResult<ProductSkuDto>.Fail(
                "Không thể tạo SKU Active trong phiên bản không hoạt động.");
        }

        if (await _context.ProductSkus.AnyAsync(sku =>
                sku.SkuCode == normalized.SkuCode))
        {
            return ServiceResult<ProductSkuDto>.Fail("Mã SKU đã tồn tại.");
        }

        var colorKey = normalized.ColorName!.ToUpper();
        if (await _context.ProductSkus.AnyAsync(sku =>
                sku.ProductVariantId == variantId &&
                sku.ColorName.ToUpper() == colorKey))
        {
            return ServiceResult<ProductSkuDto>.Fail(
                "Màu đã tồn tại trong phiên bản.");
        }

        var sku = new ProductSku
        {
            ProductVariantId = variantId,
            SkuCode = normalized.SkuCode!,
            ColorName = normalized.ColorName,
            ColorHexCode = normalized.ColorHexCode,
            Price = request.Price,
            StockQuantity = 0,
            Status = normalized.Status!
        };

        _context.ProductSkus.Add(sku);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<ProductSkuDto>.Fail(
                "Không thể tạo SKU vì mã SKU hoặc màu đã tồn tại.");
        }

        return ServiceResult<ProductSkuDto>.Success(ProductCatalogMapper.MapSku(sku));
    }

    public async Task<ServiceResult<ProductSkuDto>> UpdateAsync(
        int productId,
        int variantId,
        int skuId,
        ProductSkuUpdateRequest request)
    {
        var sku = await _context.ProductSkus
            .Include(item => item.Images)
            .Include(item => item.ProductVariant)
            .FirstOrDefaultAsync(item =>
                item.Id == skuId &&
                item.ProductVariantId == variantId &&
                item.ProductVariant.ProductId == productId);
        if (sku == null)
        {
            return ServiceResult<ProductSkuDto>.Fail("Không tìm thấy SKU.");
        }

        var normalized = ValidateAndNormalize(
            sku.SkuCode,
            request.ColorName,
            request.ColorHexCode,
            request.Price,
            request.Status);
        if (!normalized.Succeeded)
        {
            return ServiceResult<ProductSkuDto>.Fail(normalized.Error!);
        }

        if (sku.ProductVariant.Status != CatalogStatuses.Active &&
            normalized.Status == CatalogStatuses.Active)
        {
            return ServiceResult<ProductSkuDto>.Fail(
                "Không thể kích hoạt SKU khi phiên bản không hoạt động.");
        }

        byte[] clientRowVersion;
        try
        {
            clientRowVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            return ServiceResult<ProductSkuDto>.Fail("RowVersion không hợp lệ.");
        }

        if (clientRowVersion.Length != 8)
        {
            return ServiceResult<ProductSkuDto>.Fail("RowVersion không hợp lệ.");
        }

        var colorKey = normalized.ColorName!.ToUpper();
        if (await _context.ProductSkus.AnyAsync(candidate =>
                candidate.Id != skuId &&
                candidate.ProductVariantId == variantId &&
                candidate.ColorName.ToUpper() == colorKey))
        {
            return ServiceResult<ProductSkuDto>.Fail(
                "Màu đã tồn tại trong phiên bản.");
        }

        sku.ColorName = normalized.ColorName;
        sku.ColorHexCode = normalized.ColorHexCode;
        sku.Price = request.Price;
        sku.Status = normalized.Status!;
        _context.Entry(sku)
            .Property(item => item.RowVersion)
            .OriginalValue = clientRowVersion;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<ProductSkuDto>.Fail(
                "SKU đã được cập nhật bởi yêu cầu khác. Vui lòng tải lại dữ liệu.");
        }
        catch (DbUpdateException)
        {
            return ServiceResult<ProductSkuDto>.Fail(
                "Không thể cập nhật SKU vì màu đã tồn tại.");
        }

        return ServiceResult<ProductSkuDto>.Success(ProductCatalogMapper.MapSku(sku));
    }

    public async Task<ServiceResult<ProductSkuDeleteDto>> DeleteAsync(
        int productId,
        int variantId,
        int skuId)
    {
        var sku = await _context.ProductSkus
            .Include(item => item.ProductVariant)
            .FirstOrDefaultAsync(item =>
                item.Id == skuId &&
                item.ProductVariantId == variantId &&
                item.ProductVariant.ProductId == productId);
        if (sku == null)
        {
            return ServiceResult<ProductSkuDeleteDto>.Fail("Không tìm thấy SKU.");
        }

        var hasTransactions =
            await _context.OrderItems.AnyAsync(item => item.ProductSkuId == skuId) ||
            await _context.ImportReceiptDetails.AnyAsync(detail => detail.ProductSkuId == skuId);

        if (hasTransactions)
        {
            sku.Status = CatalogStatuses.Inactive;
            await _context.SaveChangesAsync();
            return ServiceResult<ProductSkuDeleteDto>.Success(new ProductSkuDeleteDto
            {
                Id = sku.Id,
                Action = "Deactivated",
                Status = sku.Status
            });
        }

        if (sku.StockQuantity > 0)
        {
            return ServiceResult<ProductSkuDeleteDto>.Fail(
                "Không thể xóa SKU khi vẫn còn tồn kho.");
        }

        if (await _context.ProductImages.AnyAsync(image => image.ProductSkuId == skuId))
        {
            return ServiceResult<ProductSkuDeleteDto>.Fail(
                "Hãy xóa toàn bộ ảnh của SKU trước khi xóa SKU.");
        }

        _context.ProductSkus.Remove(sku);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<ProductSkuDeleteDto>.Fail(
                "Không thể xóa vì SKU đã phát sinh giao dịch.");
        }

        return ServiceResult<ProductSkuDeleteDto>.Success(new ProductSkuDeleteDto
        {
            Id = skuId,
            Action = "Deleted"
        });
    }

    private IQueryable<ProductSku> SkuQuery()
    {
        return _context.ProductSkus
            .AsNoTracking()
            .Include(sku => sku.ProductVariant)
            .Include(sku => sku.Images);
    }

    private static (
        bool Succeeded,
        string? SkuCode,
        string? ColorName,
        string? ColorHexCode,
        string? Status,
        string? Error) ValidateAndNormalize(
            string skuCode,
            string colorName,
            string? colorHexCode,
            decimal price,
            string status)
    {
        var normalizedSkuCode = skuCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizedSkuCode.Length > 64 || !SkuCodePattern.IsMatch(normalizedSkuCode))
        {
            return (
                false,
                null,
                null,
                null,
                null,
                "Mã SKU chỉ được chứa chữ cái, chữ số và dấu gạch ngang.");
        }

        var normalizedColorName = colorName?.Trim() ?? string.Empty;
        if (normalizedColorName.Length == 0 || normalizedColorName.Length > 100)
        {
            return (false, null, null, null, null, "Tên màu không hợp lệ.");
        }

        var normalizedHex = string.IsNullOrWhiteSpace(colorHexCode)
            ? null
            : colorHexCode.Trim().ToUpperInvariant();
        if (normalizedHex != null && !ColorHexPattern.IsMatch(normalizedHex))
        {
            return (
                false,
                null,
                null,
                null,
                null,
                "Mã màu phải có dạng #RRGGBB hoặc #RRGGBBAA.");
        }

        if (price < 0)
        {
            return (false, null, null, null, null, "Giá SKU không được âm.");
        }

        var normalizedStatus = NormalizeStatus(status);
        return normalizedStatus == null
            ? (
                false,
                null,
                null,
                null,
                null,
                "Trạng thái phải là Active, Inactive hoặc Discontinued.")
            : (
                true,
                normalizedSkuCode,
                normalizedColorName,
                normalizedHex,
                normalizedStatus,
                null);
    }

    private static string? NormalizeStatus(string status)
    {
        if (string.Equals(status?.Trim(), CatalogStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogStatuses.Active;
        }

        if (string.Equals(status?.Trim(), CatalogStatuses.Inactive, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogStatuses.Inactive;
        }

        return string.Equals(
            status?.Trim(),
            CatalogStatuses.Discontinued,
            StringComparison.OrdinalIgnoreCase)
            ? CatalogStatuses.Discontinued
            : null;
    }
}

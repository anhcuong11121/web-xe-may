using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class ProductVariantService : IProductVariantService
{
    private static readonly Regex VersionCodePattern = new(
        @"^[A-Z0-9]+(?:-[A-Z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ApplicationDbContext _context;

    public ProductVariantService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductVariantDto>?> GetByProductIdAsync(
        int productId,
        bool includeInactive)
    {
        if (!await _context.Products.AnyAsync(product => product.Id == productId))
        {
            return null;
        }

        var variants = await VariantQuery()
            .Where(variant =>
                variant.ProductId == productId &&
                (includeInactive || variant.Status == CatalogStatuses.Active))
            .OrderBy(variant => variant.Id)
            .ToListAsync();

        return variants
            .Select(variant => ProductCatalogMapper.MapVariant(variant, includeInactive))
            .ToList();
    }

    public async Task<ProductVariantDto?> GetByIdAsync(
        int productId,
        int variantId,
        bool includeInactive)
    {
        var variant = await VariantQuery()
            .FirstOrDefaultAsync(item =>
                item.Id == variantId &&
                item.ProductId == productId &&
                (includeInactive || item.Status == CatalogStatuses.Active));

        return variant == null
            ? null
            : ProductCatalogMapper.MapVariant(variant, includeInactive);
    }

    public async Task<ServiceResult<ProductVariantDto>> CreateAsync(
        int productId,
        ProductVariantCreateRequest request)
    {
        if (!await _context.Products.AnyAsync(product => product.Id == productId))
        {
            return ServiceResult<ProductVariantDto>.Fail("Không tìm thấy sản phẩm.");
        }

        var normalized = ValidateAndNormalize(
            request.Name,
            request.VersionCode,
            request.Status,
            request.Specification);
        if (!normalized.Succeeded)
        {
            return ServiceResult<ProductVariantDto>.Fail(normalized.Error!);
        }

        if (await _context.ProductVariants.AnyAsync(variant =>
                variant.ProductId == productId &&
                variant.VersionCode == normalized.VersionCode))
        {
            return ServiceResult<ProductVariantDto>.Fail(
                "Mã phiên bản đã tồn tại trong sản phẩm.");
        }

        var variant = new ProductVariant
        {
            ProductId = productId,
            Name = normalized.Name!,
            VersionCode = normalized.VersionCode!,
            Status = normalized.Status!,
            Specification = CreateSpecification(request.Specification)
        };

        _context.ProductVariants.Add(variant);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<ProductVariantDto>.Fail(
                "Không thể tạo phiên bản. Mã phiên bản có thể đã tồn tại.");
        }

        return ServiceResult<ProductVariantDto>.Success(
            ProductCatalogMapper.MapVariant(variant, includeInactiveSkus: true));
    }

    public async Task<ServiceResult<ProductVariantDto>> UpdateAsync(
        int productId,
        int variantId,
        ProductVariantUpdateRequest request)
    {
        var variant = await _context.ProductVariants
            .Include(item => item.Specification)
            .Include(item => item.Skus)
                .ThenInclude(sku => sku.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Id == variantId && item.ProductId == productId);
        if (variant == null)
        {
            return ServiceResult<ProductVariantDto>.Fail("Không tìm thấy phiên bản.");
        }

        var normalized = ValidateAndNormalize(
            request.Name,
            variant.VersionCode,
            request.Status,
            request.Specification);
        if (!normalized.Succeeded)
        {
            return ServiceResult<ProductVariantDto>.Fail(normalized.Error!);
        }

        variant.Name = normalized.Name!;
        variant.Status = normalized.Status!;
        if (variant.Specification == null)
        {
            variant.Specification = CreateSpecification(request.Specification);
        }
        else
        {
            ApplySpecification(variant.Specification, request.Specification);
        }

        await _context.SaveChangesAsync();
        return ServiceResult<ProductVariantDto>.Success(
            ProductCatalogMapper.MapVariant(variant, includeInactiveSkus: true));
    }

    public async Task<ServiceResult<ProductVariantDto>> UpdateSpecificationAsync(
        int productId,
        int variantId,
        VariantSpecificationRequest request)
    {
        var validationError = ValidateSpecification(request);
        if (validationError != null)
        {
            return ServiceResult<ProductVariantDto>.Fail(validationError);
        }

        var variant = await _context.ProductVariants
            .Include(item => item.Specification)
            .Include(item => item.Skus)
                .ThenInclude(sku => sku.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Id == variantId && item.ProductId == productId);
        if (variant == null)
        {
            return ServiceResult<ProductVariantDto>.Fail("Không tìm thấy phiên bản.");
        }

        if (variant.Specification == null)
        {
            variant.Specification = CreateSpecification(request);
        }
        else
        {
            ApplySpecification(variant.Specification, request);
        }

        await _context.SaveChangesAsync();
        return ServiceResult<ProductVariantDto>.Success(
            ProductCatalogMapper.MapVariant(variant, includeInactiveSkus: true));
    }

    public async Task<ServiceResult<ProductVariantDeleteDto>> DeleteAsync(
        int productId,
        int variantId)
    {
        var variant = await _context.ProductVariants
            .Include(item => item.Skus)
            .FirstOrDefaultAsync(item => item.Id == variantId && item.ProductId == productId);
        if (variant == null)
        {
            return ServiceResult<ProductVariantDeleteDto>.Fail("Không tìm thấy phiên bản.");
        }

        var skuIds = variant.Skus.Select(sku => sku.Id).ToList();
        var hasSkuTransactions = skuIds.Count > 0 &&
            (await _context.OrderItems.AnyAsync(item =>
                 skuIds.Contains(item.ProductSkuId)) ||
             await _context.ImportReceiptDetails.AnyAsync(detail =>
                 skuIds.Contains(detail.ProductSkuId)));

        if (hasSkuTransactions)
        {
            variant.Status = CatalogStatuses.Inactive;
            foreach (var sku in variant.Skus)
            {
                sku.Status = CatalogStatuses.Inactive;
            }

            await _context.SaveChangesAsync();
            return ServiceResult<ProductVariantDeleteDto>.Success(new ProductVariantDeleteDto
            {
                Id = variant.Id,
                Action = "Deactivated",
                Status = variant.Status
            });
        }

        if (variant.Skus.Any(sku => sku.StockQuantity > 0))
        {
            return ServiceResult<ProductVariantDeleteDto>.Fail(
                "Không thể xóa phiên bản khi SKU vẫn còn tồn kho.");
        }

        if (skuIds.Count > 0 &&
            await _context.ProductImages.AnyAsync(image => skuIds.Contains(image.ProductSkuId)))
        {
            return ServiceResult<ProductVariantDeleteDto>.Fail(
                "Hãy xóa toàn bộ ảnh của các SKU trước khi xóa phiên bản.");
        }

        _context.ProductVariants.Remove(variant);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<ProductVariantDeleteDto>.Fail(
                "Không thể xóa vì phiên bản hoặc SKU đã phát sinh giao dịch.");
        }

        return ServiceResult<ProductVariantDeleteDto>.Success(new ProductVariantDeleteDto
        {
            Id = variantId,
            Action = "Deleted"
        });
    }

    private IQueryable<ProductVariant> VariantQuery()
    {
        return _context.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Specification)
            .Include(variant => variant.Skus)
                .ThenInclude(sku => sku.Images)
            .AsSplitQuery();
    }

    private static (
        bool Succeeded,
        string? Name,
        string? VersionCode,
        string? Status,
        string? Error) ValidateAndNormalize(
            string name,
            string versionCode,
            string status,
            VariantSpecificationRequest specification)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        if (normalizedName.Length == 0)
        {
            return (false, null, null, null, "Tên phiên bản không được để trống.");
        }

        var normalizedVersionCode = versionCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!VersionCodePattern.IsMatch(normalizedVersionCode))
        {
            return (
                false,
                null,
                null,
                null,
                "Mã phiên bản chỉ được chứa chữ cái, chữ số và dấu gạch ngang.");
        }

        var normalizedStatus = NormalizeStatus(status);
        if (normalizedStatus == null)
        {
            return (
                false,
                null,
                null,
                null,
                "Trạng thái phải là Active, Inactive hoặc Discontinued.");
        }

        var specificationError = ValidateSpecification(specification);
        return specificationError == null
            ? (true, normalizedName, normalizedVersionCode, normalizedStatus, null)
            : (false, null, null, null, specificationError);
    }

    private static string? ValidateSpecification(VariantSpecificationRequest specification)
    {
        if (specification == null)
        {
            return "Thông số kỹ thuật là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(specification.EngineType))
        {
            return "Loại động cơ không được để trống.";
        }

        return string.IsNullOrWhiteSpace(specification.FuelType)
            ? "Loại nhiên liệu không được để trống."
            : null;
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

    private static VariantSpecification CreateSpecification(
        VariantSpecificationRequest request)
    {
        var specification = new VariantSpecification();
        ApplySpecification(specification, request);
        return specification;
    }

    private static void ApplySpecification(
        VariantSpecification specification,
        VariantSpecificationRequest request)
    {
        specification.EngineType = request.EngineType.Trim();
        specification.FuelType = request.FuelType.Trim();
        specification.EngineCapacityCc = request.EngineCapacityCc;
        specification.HorsePower = request.HorsePower;
        specification.CurbWeightKg = request.CurbWeightKg;
        specification.Dimensions = NormalizeOptional(request.Dimensions);
        specification.FuelTankCapacityLiters = request.FuelTankCapacityLiters;
        specification.MaxPower = NormalizeOptional(request.MaxPower);
        specification.FuelConsumptionLitersPer100Km =
            request.FuelConsumptionLitersPer100Km;
        specification.OtherDetails = NormalizeOptional(request.OtherDetails);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}

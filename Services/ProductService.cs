using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryParameters query)
    {
        var productsQuery = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.VehicleType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            productsQuery = productsQuery.Where(p => EF.Functions.Like(p.Name, $"%{keyword}%"));
        }

        if (query.BrandId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.BrandId == query.BrandId.Value);
        }

        if (query.VehicleTypeId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.VehicleTypeId == query.VehicleTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            productsQuery = productsQuery.Where(p => p.Status == status);
        }

        if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
        {
            productsQuery = productsQuery.Where(product => product.Variants
                .Where(variant => variant.Status == CatalogStatuses.Active)
                .SelectMany(variant => variant.Skus)
                .Any(sku =>
                    sku.Status == CatalogStatuses.Active &&
                    (!query.MinPrice.HasValue || sku.Price >= query.MinPrice.Value) &&
                    (!query.MaxPrice.HasValue || sku.Price <= query.MaxPrice.Value)));
        }

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : Math.Min(query.PageSize, 100);

        var totalCount = await productsQuery.CountAsync();

        var entities = await productsQuery
            .OrderByDescending(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProductDto>
        {
            Items = entities.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.VehicleType)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : MapToDto(product);
    }

    public async Task<PagedResult<ProductCatalogSummaryDto>> GetCatalogProductsAsync(ProductQueryParameters query)
    {
        var productsQuery = _context.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            productsQuery = productsQuery.Where(product =>
                EF.Functions.Like(product.Name, $"%{keyword}%"));
        }

        if (query.BrandId.HasValue)
        {
            productsQuery = productsQuery.Where(product =>
                product.BrandId == query.BrandId.Value);
        }

        if (query.VehicleTypeId.HasValue)
        {
            productsQuery = productsQuery.Where(product =>
                product.VehicleTypeId == query.VehicleTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            productsQuery = productsQuery.Where(product => product.Status == status);
        }

        if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
        {
            productsQuery = productsQuery.Where(product => product.Variants
                .Where(variant => variant.Status == CatalogStatuses.Active)
                .SelectMany(variant => variant.Skus)
                .Any(sku =>
                    sku.Status == CatalogStatuses.Active &&
                    (!query.MinPrice.HasValue || sku.Price >= query.MinPrice.Value) &&
                    (!query.MaxPrice.HasValue || sku.Price <= query.MaxPrice.Value)));
        }

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : Math.Min(query.PageSize, 100);
        var totalCount = await productsQuery.CountAsync();

        var items = await productsQuery
            .OrderByDescending(product => product.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductCatalogSummaryDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Status = product.Status,
                BrandId = product.BrandId,
                BrandName = product.Brand.Name,
                VehicleTypeId = product.VehicleTypeId,
                VehicleTypeName = product.VehicleType == null ? null : product.VehicleType.Name,
                MinimumPrice = product.Variants
                    .Where(variant => variant.Status == CatalogStatuses.Active)
                    .SelectMany(variant => variant.Skus)
                    .Where(sku => sku.Status == CatalogStatuses.Active)
                    .Min(sku => (decimal?)sku.Price),
                MaximumPrice = product.Variants
                    .Where(variant => variant.Status == CatalogStatuses.Active)
                    .SelectMany(variant => variant.Skus)
                    .Where(sku => sku.Status == CatalogStatuses.Active)
                    .Max(sku => (decimal?)sku.Price),
                MinimumEngineCapacityCc = product.Variants
                    .Where(variant =>
                        variant.Status == CatalogStatuses.Active &&
                        variant.Specification != null)
                    .Min(variant => (int?)variant.Specification!.EngineCapacityCc),
                MaximumEngineCapacityCc = product.Variants
                    .Where(variant =>
                        variant.Status == CatalogStatuses.Active &&
                        variant.Specification != null)
                    .Max(variant => (int?)variant.Specification!.EngineCapacityCc),
                TotalStock = product.Variants
                    .Where(variant => variant.Status == CatalogStatuses.Active)
                    .SelectMany(variant => variant.Skus)
                    .Where(sku => sku.Status == CatalogStatuses.Active)
                    .Sum(sku => (long?)sku.StockQuantity) ?? 0,
                AvailableSkuCount = product.Variants
                    .Where(variant => variant.Status == CatalogStatuses.Active)
                    .SelectMany(variant => variant.Skus)
                    .Count(sku => sku.Status == CatalogStatuses.Active && sku.StockQuantity > 0),
                PrimaryImageUrl = product.Variants
                    .Where(variant => variant.Status == CatalogStatuses.Active)
                    .SelectMany(variant => variant.Skus)
                    .Where(sku => sku.Status == CatalogStatuses.Active)
                    .SelectMany(sku => sku.Images)
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.DisplayOrder)
                    .ThenBy(image => image.ProductSkuId)
                    .ThenBy(image => image.Id)
                    .Select(image => image.Url)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return new PagedResult<ProductCatalogSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ProductCatalogDetailDto?> GetProductCatalogByIdAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(item => item.Brand)
            .Include(item => item.VehicleType)
            .Include(item => item.Variants)
                .ThenInclude(variant => variant.Specification)
            .Include(item => item.Variants)
                .ThenInclude(variant => variant.Skus)
                    .ThenInclude(sku => sku.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Id == id);

        return product == null ? null : MapToCatalogDetailDto(product);
    }

    public async Task<bool> RecordInterestAsync(int id)
    {
        if (!await _context.Products.AnyAsync(product => product.Id == id)) return false;
        _context.ProductInterests.Add(new ProductInterest { ProductId = id });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ServiceResult<ProductDto>> CreateProductAsync(ProductCreateRequest request)
    {
        var brandExists = await _context.Brands.AnyAsync(b => b.Id == request.BrandId);
        if (!brandExists)
        {
            return ServiceResult<ProductDto>.Fail("BrandId không tồn tại.");
        }

        if (request.VehicleTypeId.HasValue &&
            !await _context.VehicleTypes.AnyAsync(v => v.Id == request.VehicleTypeId.Value))
        {
            return ServiceResult<ProductDto>.Fail("VehicleTypeId không tồn tại.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Status = request.Status,
            BrandId = request.BrandId,
            VehicleTypeId = request.VehicleTypeId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _context.Entry(product).Reference(p => p.Brand).LoadAsync();
        if (product.VehicleTypeId.HasValue)
        {
            await _context.Entry(product).Reference(p => p.VehicleType).LoadAsync();
        }

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> UpdateProductAsync(int id, ProductUpdateRequest request)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.VehicleType)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return ServiceResult<ProductDto>.Fail("Không tìm thấy sản phẩm.");
        }

        var brandExists = await _context.Brands.AnyAsync(b => b.Id == request.BrandId);
        if (!brandExists)
        {
            return ServiceResult<ProductDto>.Fail("BrandId không tồn tại.");
        }

        if (request.VehicleTypeId.HasValue &&
            !await _context.VehicleTypes.AnyAsync(v => v.Id == request.VehicleTypeId.Value))
        {
            return ServiceResult<ProductDto>.Fail("VehicleTypeId không tồn tại.");
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Status = request.Status;
        product.VehicleTypeId = request.VehicleTypeId;

        product.BrandId = request.BrandId;
        await _context.SaveChangesAsync();

        var updatedProduct = await GetProductByIdAsync(product.Id);
        return ServiceResult<ProductDto>.Success(updatedProduct!);
    }

    public async Task<ServiceResult<bool>> DeleteProductAsync(int id)
    {
        var product = await _context.Products
            .Include(item => item.Variants)
                .ThenInclude(variant => variant.Skus)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (product == null)
        {
            return ServiceResult<bool>.Fail("Không tìm thấy sản phẩm.");
        }

        var skus = product.Variants.SelectMany(variant => variant.Skus).ToList();
        if (skus.Any(sku => sku.StockQuantity > 0))
        {
            return ServiceResult<bool>.Fail(
                "Không thể xóa sản phẩm khi SKU vẫn còn tồn kho.");
        }

        var skuIds = skus.Select(sku => sku.Id).ToList();
        if (skuIds.Count > 0 &&
            await _context.ProductImages.AnyAsync(image => skuIds.Contains(image.ProductSkuId)))
        {
            return ServiceResult<bool>.Fail(
                "Hãy xóa toàn bộ ảnh của các SKU trước khi xóa sản phẩm.");
        }

        _context.Products.Remove(product);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Product đang bị tham chiếu bởi OrderItem/ImportReceiptDetail (DeleteBehavior.Restrict).
            return ServiceResult<bool>.Fail("Không thể xóa vì sản phẩm đã phát sinh đơn hàng hoặc phiếu nhập liên quan.");
        }

        return ServiceResult<bool>.Success(true);
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Status = product.Status,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name ?? string.Empty,
            VehicleTypeId = product.VehicleTypeId,
            VehicleTypeName = product.VehicleType?.Name
        };
    }

    private static ProductCatalogDetailDto MapToCatalogDetailDto(Product product)
    {
        var activeVariants = product.Variants
            .Where(variant => variant.Status == CatalogStatuses.Active)
            .OrderBy(variant => variant.Id)
            .ToList();
        var activeSkus = activeVariants
            .SelectMany(variant => variant.Skus)
            .Where(sku => sku.Status == CatalogStatuses.Active)
            .ToList();

        return new ProductCatalogDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Status = product.Status,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name ?? string.Empty,
            VehicleTypeId = product.VehicleTypeId,
            VehicleTypeName = product.VehicleType?.Name,
            MinimumPrice = activeSkus.Count == 0 ? null : activeSkus.Min(sku => sku.Price),
            MaximumPrice = activeSkus.Count == 0 ? null : activeSkus.Max(sku => sku.Price),
            MinimumEngineCapacityCc = activeVariants
                .Where(variant => variant.Specification != null)
                .Select(variant => (int?)variant.Specification!.EngineCapacityCc)
                .Min(),
            MaximumEngineCapacityCc = activeVariants
                .Where(variant => variant.Specification != null)
                .Select(variant => (int?)variant.Specification!.EngineCapacityCc)
                .Max(),
            TotalStock = activeSkus.Sum(sku => (long)sku.StockQuantity),
            AvailableSkuCount = activeSkus.Count(sku => sku.StockQuantity > 0),
            PrimaryImageUrl = activeSkus
                .SelectMany(sku => sku.Images)
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.ProductSkuId)
                .ThenBy(image => image.Id)
                .Select(image => image.Url)
                .FirstOrDefault(),
            Variants = activeVariants
                .Select(variant => ProductCatalogMapper.MapVariant(
                    variant,
                    includeInactiveSkus: false))
                .ToList()
        };
    }
}

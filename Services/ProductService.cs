using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class ProductService : IProductService
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".png", ".webp" };
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProductService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryParameters query)
    {
        var productsQuery = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.VehicleType)
            .Include(p => p.Specification)
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

        if (query.MinPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price <= query.MaxPrice.Value);
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
            .Include(p => p.Specification)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : MapToDto(product);
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
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Color = request.Color,
            Status = request.Status,
            BrandId = request.BrandId,
            VehicleTypeId = request.VehicleTypeId,
            Specification = new Specification
            {
                EngineType = request.Specification.EngineType,
                FuelType = request.Specification.FuelType,
                EngineCapacityCc = request.Specification.EngineCapacityCc,
                HorsePower = request.Specification.HorsePower,
                CurbWeightKg = request.Specification.CurbWeightKg,
                Dimensions = request.Specification.Dimensions,
                FuelTankCapacityLiters = request.Specification.FuelTankCapacityLiters,
                MaxPower = request.Specification.MaxPower,
                FuelConsumptionLitersPer100Km = request.Specification.FuelConsumptionLitersPer100Km,
                OtherDetails = request.Specification.OtherDetails
            }
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
            .Include(p => p.Specification)
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

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.Color = request.Color;
        product.Status = request.Status;
        product.VehicleTypeId = request.VehicleTypeId;

        if (product.Specification == null)
        {
            product.Specification = new Specification { ProductId = product.Id };
        }

        product.Specification.EngineType = request.Specification.EngineType;
        product.Specification.FuelType = request.Specification.FuelType;
        product.Specification.EngineCapacityCc = request.Specification.EngineCapacityCc;
        product.Specification.HorsePower = request.Specification.HorsePower;
        product.Specification.CurbWeightKg = request.Specification.CurbWeightKg;
        product.Specification.Dimensions = request.Specification.Dimensions;
        product.Specification.FuelTankCapacityLiters = request.Specification.FuelTankCapacityLiters;
        product.Specification.MaxPower = request.Specification.MaxPower;
        product.Specification.FuelConsumptionLitersPer100Km = request.Specification.FuelConsumptionLitersPer100Km;
        product.Specification.OtherDetails = request.Specification.OtherDetails;

        product.BrandId = request.BrandId;
        await _context.SaveChangesAsync();

        var updatedProduct = await GetProductByIdAsync(product.Id);
        return ServiceResult<ProductDto>.Success(updatedProduct!);
    }

    public async Task<ServiceResult<bool>> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return ServiceResult<bool>.Fail("Không tìm thấy sản phẩm.");
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

    public async Task<ServiceResult<ProductDto>> UploadProductImageAsync(int id, IFormFile file)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.VehicleType)
            .Include(p => p.Specification)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return ServiceResult<ProductDto>.Fail("Không tìm thấy sản phẩm.");
        }

        if (file == null || file.Length == 0)
        {
            return ServiceResult<ProductDto>.Fail("Vui lòng chọn file ảnh.");
        }

        if (file.Length > MaxImageSizeBytes)
        {
            return ServiceResult<ProductDto>.Fail("Kích thước ảnh vượt quá 5MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            return ServiceResult<ProductDto>.Fail("Định dạng ảnh không hợp lệ (chỉ chấp nhận .jpg, .png và .webp).");
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "products");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        product.ImageUrl = $"/uploads/products/{fileName}";
        await _context.SaveChangesAsync();

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Color = product.Color,
            Status = product.Status,
            ImageUrl = product.ImageUrl,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name ?? string.Empty,
            VehicleTypeId = product.VehicleTypeId,
            VehicleTypeName = product.VehicleType?.Name,
            Specification = product.Specification == null ? null : new SpecificationDto
            {
                EngineType = product.Specification.EngineType,
                FuelType = product.Specification.FuelType,
                EngineCapacityCc = product.Specification.EngineCapacityCc,
                HorsePower = product.Specification.HorsePower,
                CurbWeightKg = product.Specification.CurbWeightKg,
                Dimensions = product.Specification.Dimensions,
                FuelTankCapacityLiters = product.Specification.FuelTankCapacityLiters,
                MaxPower = product.Specification.MaxPower,
                FuelConsumptionLitersPer100Km = product.Specification.FuelConsumptionLitersPer100Km,
                OtherDetails = product.Specification.OtherDetails
            }
        };
    }
}

using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class ProductImageService : IProductImageService
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private const string ManagedUrlPrefix = "/uploads/products/skus/";
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".png",
            ".webp"
        };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProductImageService(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<List<ProductImageDto>?> GetBySkuAsync(
        int productId,
        int variantId,
        int skuId,
        bool includeInactive)
    {
        var skuExists = await _context.ProductSkus
            .AsNoTracking()
            .AnyAsync(sku =>
                sku.Id == skuId &&
                sku.ProductVariantId == variantId &&
                sku.ProductVariant.ProductId == productId &&
                (includeInactive ||
                 (sku.Status == CatalogStatuses.Active &&
                  sku.ProductVariant.Status == CatalogStatuses.Active)));
        if (!skuExists)
        {
            return null;
        }

        return await _context.ProductImages
            .AsNoTracking()
            .Where(image => image.ProductSkuId == skuId)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .Select(image => new ProductImageDto
            {
                Id = image.Id,
                ProductSkuId = image.ProductSkuId,
                Url = image.Url,
                AltText = image.AltText,
                DisplayOrder = image.DisplayOrder,
                IsPrimary = image.IsPrimary
            })
            .ToListAsync();
    }

    public async Task<ServiceResult<ProductImageDto>> UploadAsync(
        int productId,
        int variantId,
        int skuId,
        ProductImageUploadRequest request)
    {
        if (!await SkuExistsAsync(productId, variantId, skuId))
        {
            return ServiceResult<ProductImageDto>.Fail("Không tìm thấy SKU.");
        }

        var validationError = ValidateFile(request.File);
        if (validationError != null)
        {
            return ServiceResult<ProductImageDto>.Fail(validationError);
        }

        var altText = NormalizeAltText(request.AltText);
        if (!altText.Succeeded)
        {
            return ServiceResult<ProductImageDto>.Fail(altText.Error!);
        }

        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var uploadsFolder = GetManagedUploadsFolder();
        var filePath = Path.Combine(uploadsFolder, fileName);

        try
        {
            Directory.CreateDirectory(uploadsFolder);
            await using var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            await request.File.CopyToAsync(stream);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            TryDeleteFile(filePath);
            return ServiceResult<ProductImageDto>.Fail("Không thể lưu file ảnh.");
        }

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await BeginSerializableTransactionAsync();
            var existingImages = await _context.ProductImages
                .Where(image => image.ProductSkuId == skuId)
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .ToListAsync();
            var makePrimary = request.IsPrimary ||
                              existingImages.All(image => !image.IsPrimary);

            if (makePrimary)
            {
                foreach (var existingImage in existingImages.Where(image => image.IsPrimary))
                {
                    existingImage.IsPrimary = false;
                }

                await _context.SaveChangesAsync();
            }

            var image = new ProductImage
            {
                ProductSkuId = skuId,
                Url = $"{ManagedUrlPrefix}{fileName}",
                AltText = altText.Value,
                DisplayOrder = request.DisplayOrder,
                IsPrimary = makePrimary
            };
            _context.ProductImages.Add(image);
            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return ServiceResult<ProductImageDto>.Success(
                ProductCatalogMapper.MapImage(image));
        }
        catch (DbUpdateException)
        {
            await TryRollbackAsync(transaction);
            _context.ChangeTracker.Clear();
            TryDeleteFile(filePath);
            return ServiceResult<ProductImageDto>.Fail(
                "Không thể lưu ảnh; SKU đã có ảnh chính được cập nhật đồng thời.");
        }
        catch
        {
            await TryRollbackAsync(transaction);
            _context.ChangeTracker.Clear();
            TryDeleteFile(filePath);
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<ServiceResult<ProductImageDto>> UpdateAsync(
        int productId,
        int variantId,
        int skuId,
        int imageId,
        ProductImageUpdateRequest request)
    {
        if (!await SkuExistsAsync(productId, variantId, skuId))
        {
            return ServiceResult<ProductImageDto>.Fail("Không tìm thấy SKU.");
        }

        var altText = NormalizeAltText(request.AltText);
        if (!altText.Succeeded)
        {
            return ServiceResult<ProductImageDto>.Fail(altText.Error!);
        }

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await BeginSerializableTransactionAsync();
            var images = await _context.ProductImages
                .Where(image => image.ProductSkuId == skuId)
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .ToListAsync();
            var image = images.FirstOrDefault(candidate => candidate.Id == imageId);
            if (image == null)
            {
                return ServiceResult<ProductImageDto>.Fail("Không tìm thấy ảnh.");
            }

            image.AltText = altText.Value;
            image.DisplayOrder = request.DisplayOrder;

            if (request.IsPrimary && !image.IsPrimary)
            {
                foreach (var currentPrimary in images.Where(candidate => candidate.IsPrimary))
                {
                    currentPrimary.IsPrimary = false;
                }

                await _context.SaveChangesAsync();
                image.IsPrimary = true;
            }
            else if (!request.IsPrimary && image.IsPrimary)
            {
                var replacement = images.FirstOrDefault(candidate => candidate.Id != image.Id);
                if (replacement != null)
                {
                    image.IsPrimary = false;
                    await _context.SaveChangesAsync();
                    replacement.IsPrimary = true;
                }
            }

            if (images.All(candidate => !candidate.IsPrimary))
            {
                images[0].IsPrimary = true;
            }

            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return ServiceResult<ProductImageDto>.Success(
                ProductCatalogMapper.MapImage(image));
        }
        catch (DbUpdateException)
        {
            await TryRollbackAsync(transaction);
            _context.ChangeTracker.Clear();
            return ServiceResult<ProductImageDto>.Fail(
                "Không thể cập nhật ảnh chính do có yêu cầu đồng thời.");
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<ServiceResult<ProductImageDeleteDto>> DeleteAsync(
        int productId,
        int variantId,
        int skuId,
        int imageId)
    {
        if (!await SkuExistsAsync(productId, variantId, skuId))
        {
            return ServiceResult<ProductImageDeleteDto>.Fail("Không tìm thấy SKU.");
        }

        var images = await _context.ProductImages
            .Where(image => image.ProductSkuId == skuId)
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .ToListAsync();
        var image = images.FirstOrDefault(candidate => candidate.Id == imageId);
        if (image == null)
        {
            return ServiceResult<ProductImageDeleteDto>.Fail("Không tìm thấy ảnh.");
        }

        var originalPath = TryResolveManagedFilePath(image.Url);
        string? stagedPath = null;
        if (originalPath != null && File.Exists(originalPath))
        {
            stagedPath = $"{originalPath}.deleting-{Guid.NewGuid():N}";
            try
            {
                File.Move(originalPath, stagedPath);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                return ServiceResult<ProductImageDeleteDto>.Fail(
                    "Không thể chuẩn bị xóa file ảnh.");
            }
        }

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await BeginSerializableTransactionAsync();
            int? promotedImageId = null;
            if (image.IsPrimary)
            {
                image.IsPrimary = false;
                await _context.SaveChangesAsync();

                var replacement = images.FirstOrDefault(candidate => candidate.Id != image.Id);
                if (replacement != null)
                {
                    replacement.IsPrimary = true;
                    promotedImageId = replacement.Id;
                }
            }
            else if (images.All(candidate => !candidate.IsPrimary))
            {
                var replacement = images.FirstOrDefault(candidate => candidate.Id != image.Id);
                if (replacement != null)
                {
                    replacement.IsPrimary = true;
                    promotedImageId = replacement.Id;
                }
            }

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            if (stagedPath != null)
            {
                TryDeleteFile(stagedPath);
            }

            return ServiceResult<ProductImageDeleteDto>.Success(new ProductImageDeleteDto
            {
                Id = imageId,
                ProductSkuId = skuId,
                PromotedImageId = promotedImageId
            });
        }
        catch (DbUpdateException)
        {
            await TryRollbackAsync(transaction);
            _context.ChangeTracker.Clear();
            TryRestoreFile(stagedPath, originalPath);
            return ServiceResult<ProductImageDeleteDto>.Fail("Không thể xóa ảnh.");
        }
        catch
        {
            await TryRollbackAsync(transaction);
            _context.ChangeTracker.Clear();
            TryRestoreFile(stagedPath, originalPath);
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private Task<bool> SkuExistsAsync(int productId, int variantId, int skuId)
    {
        return _context.ProductSkus.AnyAsync(sku =>
            sku.Id == skuId &&
            sku.ProductVariantId == variantId &&
            sku.ProductVariant.ProductId == productId);
    }

    private async Task<IDbContextTransaction?> BeginSerializableTransactionAsync()
    {
        return _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
    }

    private static async Task TryRollbackAsync(IDbContextTransaction? transaction)
    {
        if (transaction == null)
        {
            return;
        }

        try
        {
            await transaction.RollbackAsync();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private string GetManagedUploadsFolder()
    {
        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        return Path.Combine(webRoot, "uploads", "products", "skus");
    }

    private string? TryResolveManagedFilePath(string url)
    {
        if (!url.StartsWith(ManagedUrlPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var fileName = url[ManagedUrlPrefix.Length..];
        if (fileName.Length == 0 ||
            fileName != Path.GetFileName(fileName))
        {
            return null;
        }

        return Path.Combine(GetManagedUploadsFolder(), fileName);
    }

    private static string? ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "Vui lòng chọn file ảnh.";
        }

        if (file.Length > MaxImageSizeBytes)
        {
            return "Kích thước ảnh vượt quá 5MB.";
        }

        var extension = Path.GetExtension(file.FileName);
        return AllowedImageExtensions.Contains(extension)
            ? null
            : "Định dạng ảnh không hợp lệ (chỉ chấp nhận .jpg, .png và .webp).";
    }

    private static (bool Succeeded, string? Value, string? Error) NormalizeAltText(
        string? altText)
    {
        var normalized = string.IsNullOrWhiteSpace(altText)
            ? null
            : altText.Trim();
        return normalized?.Length > 200
            ? (false, null, "Mô tả ảnh không được vượt quá 200 ký tự.")
            : (true, normalized, null);
    }

    private static void TryDeleteFile(string? path)
    {
        if (path == null)
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryRestoreFile(string? stagedPath, string? originalPath)
    {
        if (stagedPath == null ||
            originalPath == null ||
            !File.Exists(stagedPath) ||
            File.Exists(originalPath))
        {
            return;
        }

        try
        {
            File.Move(stagedPath, originalPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

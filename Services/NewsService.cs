using Microsoft.EntityFrameworkCore;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

public class NewsService : INewsService
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedStatuses = { "Draft", "Published", "Archived" };
    private static readonly string[] AllowedContentTypes = { "News", "Promotion" };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public NewsService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<List<NewsDto>> GetAllAsync(bool includeUnpublished = false)
    {
        var query = _context.News
            .Include(n => n.Author)
            .AsQueryable();

        if (!includeUnpublished)
        {
            query = query.Where(n => n.Status == "Published");
        }

        var news = await query
            .OrderByDescending(n => n.PublishedAt)
            .ToListAsync();

        return news.Select(MapToDto).ToList();
    }

    public async Task<NewsDto?> GetByIdAsync(int id, bool includeUnpublished = false)
    {
        var query = _context.News.Include(n => n.Author).AsQueryable();
        if (!includeUnpublished)
        {
            query = query.Where(n => n.Status == "Published");
        }

        var news = await query.FirstOrDefaultAsync(n => n.Id == id);
        return news == null ? null : MapToDto(news);
    }

    public async Task<ServiceResult<NewsDto>> CreateAsync(Guid authorId, NewsCreateRequest request)
    {
        var validationError = ValidateClassification(request.Status, request.ContentType);
        if (validationError != null)
        {
            return ServiceResult<NewsDto>.Fail(validationError);
        }

        var news = new News
        {
            Title = request.Title,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            Status = request.Status,
            ContentType = request.ContentType,
            PublishedAt = request.Status == "Published" ? DateTime.UtcNow : null,
            AuthorId = authorId
        };

        _context.News.Add(news);
        await _context.SaveChangesAsync();

        await _context.Entry(news).Reference(n => n.Author).LoadAsync();

        return ServiceResult<NewsDto>.Success(MapToDto(news));
    }

    public async Task<ServiceResult<NewsDto>> UpdateAsync(int id, NewsUpdateRequest request)
    {
        var validationError = ValidateClassification(request.Status, request.ContentType);
        if (validationError != null)
        {
            return ServiceResult<NewsDto>.Fail(validationError);
        }

        var news = await _context.News.Include(n => n.Author).FirstOrDefaultAsync(n => n.Id == id);
        if (news == null)
        {
            return ServiceResult<NewsDto>.Fail("Không tìm thấy tin tức.");
        }

        news.Title = request.Title;
        news.Content = request.Content;
        news.ImageUrl = request.ImageUrl;
        news.ContentType = request.ContentType;
        if (request.Status == "Published" && news.PublishedAt == null)
        {
            news.PublishedAt = DateTime.UtcNow;
        }
        news.Status = request.Status;

        await _context.SaveChangesAsync();

        return ServiceResult<NewsDto>.Success(MapToDto(news));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        var news = await _context.News.FindAsync(id);
        if (news == null)
        {
            return ServiceResult<bool>.Fail("Không tìm thấy tin tức.");
        }

        _context.News.Remove(news);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<NewsDto>> UploadImageAsync(int id, IFormFile file)
    {
        var news = await _context.News.Include(n => n.Author).FirstOrDefaultAsync(n => n.Id == id);
        if (news == null) return ServiceResult<NewsDto>.Fail("Không tìm thấy tin tức.");
        if (file == null || file.Length == 0) return ServiceResult<NewsDto>.Fail("Vui lòng chọn file ảnh.");
        if (file.Length > MaxImageSizeBytes) return ServiceResult<NewsDto>.Fail("Kích thước ảnh vượt quá 5MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
            return ServiceResult<NewsDto>.Fail("Chỉ chấp nhận ảnh JPG, PNG hoặc WebP.");

        var folder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "news");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid()}{extension}";
        await using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        news.ImageUrl = $"/uploads/news/{fileName}";
        await _context.SaveChangesAsync();
        return ServiceResult<NewsDto>.Success(MapToDto(news));
    }

    private static NewsDto MapToDto(News news)
    {
        return new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content,
            ImageUrl = news.ImageUrl,
            Status = news.Status,
            ContentType = news.ContentType,
            PublishedAt = news.PublishedAt,
            AuthorId = news.AuthorId,
            AuthorName = news.Author?.FullName ?? string.Empty
        };
    }

    private static string? ValidateClassification(string status, string contentType)
    {
        if (!AllowedStatuses.Contains(status))
        {
            return $"Status không hợp lệ. Cho phép: {string.Join(", ", AllowedStatuses)}.";
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            return $"ContentType không hợp lệ. Cho phép: {string.Join(", ", AllowedContentTypes)}.";
        }

        return null;
    }
}

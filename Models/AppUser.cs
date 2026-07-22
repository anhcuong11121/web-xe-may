using Microsoft.AspNetCore.Identity;
using MotorBikeShop.API.Models;
using System.ComponentModel.DataAnnotations;
public class AppUser : IdentityUser<Guid>
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual CustomerProfile? CustomerProfile { get; set; }

    public virtual EmployeeProfile? EmployeeProfile { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();

    public virtual ICollection<SupportRequest> AssignedSupportRequests { get; set; } = new List<SupportRequest>();

    public virtual ICollection<News> NewsArticles { get; set; } = new List<News>();

    public virtual ICollection<PaymentAttempt> ProcessedPaymentAttempts { get; set; } = new List<PaymentAttempt>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

namespace MotorBikeShop.API.DTOs;

public class SupportRequestDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string SupportType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Response { get; set; }
    public DateTime? RespondedAt { get; set; }
    public Guid? AssignedEmployeeUserId { get; set; }
    public string? AssignedEmployeeName { get; set; }
}

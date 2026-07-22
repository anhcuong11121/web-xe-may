using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Services;

public interface IPaymentService
{
    Task<ServiceResult<PaymentAttemptDto>> InitiateAsync(Guid currentUserId, PaymentInitiateRequest request);
    Task<ServiceResult<PaymentConfirmationDto>> ConfirmFakeAsync(Guid id, Guid currentUserId);
    Task<ServiceResult<PaymentAttemptDto>> FailFakeAsync(Guid id, Guid currentUserId);
    Task<ServiceResult<PaymentConfirmationDto>> CompleteManualAsync(Guid id, Guid processedByUserId);
    Task<PagedResult<PaymentAttemptDto>> GetListAsync(
        PaymentAttemptQueryParameters query,
        Guid currentUserId,
        string currentUserRole);
    Task<PaymentAttemptDto?> GetByIdAsync(Guid id, Guid currentUserId, string currentUserRole);
}

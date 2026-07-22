namespace MotorBikeShop.API.Services;

/// <summary>
/// Kết quả trả về dùng chung cho các Service nghiệp vụ (Product, Brand, Order, ...),
/// giúp Controller không phải tự suy luận business logic hay dùng try/catch.
/// </summary>
public class ServiceResult<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }

    public static ServiceResult<T> Success(T data) => new()
    {
        Succeeded = true,
        Data = data
    };

    public static ServiceResult<T> Fail(string error) => new()
    {
        Succeeded = false,
        Error = error
    };
}

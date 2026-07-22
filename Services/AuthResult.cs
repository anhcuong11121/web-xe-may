namespace MotorBikeShop.API.Services;

/// <summary>
/// Kết quả trả về từ AuthService cho Controller, tránh việc Controller phải
/// tự suy luận business logic (VD: email trùng, sai mật khẩu...).
/// </summary>
public class AuthResult<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();

    public static AuthResult<T> Success(T data) => new()
    {
        Succeeded = true,
        Data = data
    };

    public static AuthResult<T> Fail(params string[] errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}

using System.ComponentModel.DataAnnotations;
using MotorBikeShop.API.DTOs;

namespace MotorBikeShop.API.Tests;

public class DtoValidationTests
{
    [Fact]
    public void RegisterRequest_StrongPassword_IsValid()
    {
        var request = new RegisterRequest
        {
            Email = "customer@example.com",
            FullName = "Khách hàng",
            PhoneNumber = "0987654321",
            Password = "StrongPass1!",
            ConfirmPassword = "StrongPass1!"
        };

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("nouppercase1!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("NoNumber!")]
    [InlineData("NoSpecial1")]
    public void RegisterRequest_WeakPassword_IsInvalid(string password)
    {
        var request = new RegisterRequest
        {
            Email = "customer@example.com",
            FullName = "Khách hàng",
            PhoneNumber = "0987654321",
            Password = password,
            ConfirmPassword = password
        };

        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void ChangePasswordRequest_MismatchedConfirmation_IsInvalid()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword1!",
            NewPassword = "NewPassword1!",
            ConfirmPassword = "Different1!"
        };

        Assert.Contains(Validate(request), result =>
            result.MemberNames.Contains(nameof(ChangePasswordRequest.ConfirmPassword)));
    }

    [Fact]
    public void PaymentQuery_UnsupportedFilters_AreInvalid()
    {
        var query = new PaymentAttemptQueryParameters
        {
            PageNumber = 0,
            PageSize = 101,
            Status = "Unknown",
            PaymentMethod = "Crypto"
        };

        Assert.Equal(4, Validate(query).Count);
    }

    private static List<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
        return results;
    }
}

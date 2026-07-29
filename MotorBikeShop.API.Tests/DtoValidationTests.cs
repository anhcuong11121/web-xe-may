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

    [Fact]
    public void ProductVariantRequests_EnforceRequiredLengthsAndNonNegativeSpecification()
    {
        var request = new ProductVariantCreateRequest
        {
            Name = string.Empty,
            VersionCode = new string('A', 65),
            Status = new string('A', 33),
            Specification = new VariantSpecificationRequest
            {
                EngineType = string.Empty,
                FuelType = string.Empty,
                EngineCapacityCc = -1,
                HorsePower = -1
            }
        };

        Assert.Equal(3, Validate(request).Count);
        Assert.Equal(4, Validate(request.Specification).Count);
    }

    [Fact]
    public void ProductSkuRequests_ValidateInputAndDoNotExposeStockMutation()
    {
        var createRequest = new ProductSkuCreateRequest
        {
            SkuCode = string.Empty,
            ColorName = string.Empty,
            ColorHexCode = new string('A', 10),
            Price = -1,
            Status = string.Empty
        };
        var updateRequest = new ProductSkuUpdateRequest
        {
            ColorName = string.Empty,
            Price = -1,
            Status = string.Empty,
            RowVersion = string.Empty
        };

        Assert.Equal(5, Validate(createRequest).Count);
        Assert.Equal(4, Validate(updateRequest).Count);
        Assert.Null(typeof(ProductSkuCreateRequest).GetProperty("StockQuantity"));
        Assert.Null(typeof(ProductSkuUpdateRequest).GetProperty("StockQuantity"));
        Assert.Null(typeof(ProductSkuUpdateRequest).GetProperty("SkuCode"));
    }

    [Fact]
    public void ProductImageRequests_ValidateFileMetadata()
    {
        var uploadRequest = new ProductImageUploadRequest
        {
            File = null!,
            AltText = new string('A', 201),
            DisplayOrder = -1
        };
        var updateRequest = new ProductImageUpdateRequest
        {
            AltText = new string('A', 201),
            DisplayOrder = -1
        };

        Assert.Equal(3, Validate(uploadRequest).Count);
        Assert.Equal(2, Validate(updateRequest).Count);
    }

    private static List<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
        return results;
    }
}

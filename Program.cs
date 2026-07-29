using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MotorBikeShop.API.Data;
using MotorBikeShop.API.Models;
using MotorBikeShop.API.Middleware;
using MotorBikeShop.API.Services;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("Token", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/429",
            title = "Quá nhiều yêu cầu.",
            status = StatusCodes.Status429TooManyRequests,
            detail = "Vui lòng chờ trước khi thử lại.",
            traceId = context.HttpContext.TraceIdentifier
        }, cancellationToken);
    };
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
    .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                     (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
    .Select(origin => origin.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins phải có ít nhất một HTTP/HTTPS origin hợp lệ.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer <token>'"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc, null),
            new List<string>()
        }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductVariantService, ProductVariantService>();
builder.Services.AddScoped<IProductSkuService, ProductSkuService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ISupportRequestService, SupportRequestService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDepositService, DepositService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IImportReceiptService, ImportReceiptService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IVehicleTypeService, VehicleTypeService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings is null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException(
        "Thiếu JwtSettings:SecretKey. Hãy cấu hình bằng user-secrets hoặc biến môi trường JwtSettings__SecretKey.");
}

if (Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < 32)
{
    throw new InvalidOperationException("JwtSettings:SecretKey phải có ít nhất 32 byte.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException("JwtSettings:Issuer và JwtSettings:Audience là bắt buộc.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenRole = context.Principal?.FindFirstValue(ClaimTypes.Role);
                var tokenSecurityStamp = context.Principal?.FindFirstValue("SecurityStamp");
                if (!Guid.TryParse(userIdValue, out var userId) ||
                    string.IsNullOrWhiteSpace(tokenRole) ||
                    string.IsNullOrWhiteSpace(tokenSecurityStamp))
                {
                    context.Fail("JWT thiếu claim định danh hoặc role.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null || !user.IsActive || await userManager.IsLockedOutAsync(user))
                {
                    context.Fail("Tài khoản không tồn tại hoặc đang bị khóa.");
                    return;
                }

                if (!string.Equals(user.SecurityStamp, tokenSecurityStamp, StringComparison.Ordinal))
                {
                    context.Fail("Phiên đăng nhập không còn hiệu lực.");
                    return;
                }

                var roles = await userManager.GetRolesAsync(user);
                if (!roles.Contains(tokenRole))
                {
                    context.Fail("Quyền trong JWT không còn hiệu lực.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var roles = new[] { "Admin", "Employee", "Customer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Không thể tạo role hệ thống '{role}': " +
                    string.Join("; ", createRoleResult.Errors.Select(error => error.Description)));
            }
        }
    }

    // Chỉ bootstrap Admin khi deployment cung cấp thông tin qua configuration
    // (khuyến nghị dùng BootstrapAdmin__Email và BootstrapAdmin__Password).
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var existingAdmins = await userManager.GetUsersInRoleAsync("Admin");
    if (!existingAdmins.Any(admin => admin.IsActive))
    {
        var bootstrapAdminEmail = app.Configuration["BootstrapAdmin:Email"];
        var bootstrapAdminPassword = app.Configuration["BootstrapAdmin:Password"];
        var bootstrapAdminFullName = app.Configuration["BootstrapAdmin:FullName"] ?? "Quản trị viên";

        if (string.IsNullOrWhiteSpace(bootstrapAdminEmail) || string.IsNullOrWhiteSpace(bootstrapAdminPassword))
        {
            app.Logger.LogWarning(
                "Hệ thống chưa có Admin. Hãy cấu hình BootstrapAdmin__Email và BootstrapAdmin__Password để tạo Admin ban đầu.");
        }
        else
        {
            var adminUser = await userManager.FindByEmailAsync(bootstrapAdminEmail);
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = bootstrapAdminEmail,
                    Email = bootstrapAdminEmail,
                    FullName = bootstrapAdminFullName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, bootstrapAdminPassword);
                if (!createResult.Succeeded)
                {
                    app.Logger.LogError(
                        "Không thể tạo Admin ban đầu cho {Email}: {Errors}",
                        bootstrapAdminEmail,
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    adminUser = null;
                }
            }

            if (adminUser != null && !adminUser.IsActive)
            {
                adminUser.IsActive = true;
                var activateResult = await userManager.UpdateAsync(adminUser);
                if (!activateResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Không thể kích hoạt Admin bootstrap '{bootstrapAdminEmail}': " +
                        string.Join("; ", activateResult.Errors.Select(error => error.Description)));
                }

                var clearLockoutResult = await userManager.SetLockoutEndDateAsync(adminUser, null);
                if (!clearLockoutResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Không thể xóa lockout cho Admin bootstrap '{bootstrapAdminEmail}': " +
                        string.Join("; ", clearLockoutResult.Errors.Select(error => error.Description)));
                }

                var resetFailedCountResult = await userManager.ResetAccessFailedCountAsync(adminUser);
                if (!resetFailedCountResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Không thể reset số lần đăng nhập sai cho Admin bootstrap '{bootstrapAdminEmail}': " +
                        string.Join("; ", resetFailedCountResult.Errors.Select(error => error.Description)));
                }

                var securityStampResult = await userManager.UpdateSecurityStampAsync(adminUser);
                if (!securityStampResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Không thể làm mới security stamp cho Admin bootstrap '{bootstrapAdminEmail}': " +
                        string.Join("; ", securityStampResult.Errors.Select(error => error.Description)));
                }
            }

            if (adminUser != null)
            {
                var currentBootstrapRoles = await userManager.GetRolesAsync(adminUser);
                var nonAdminRoles = currentBootstrapRoles
                    .Where(role => !string.Equals(role, "Admin", StringComparison.Ordinal))
                    .ToArray();
                if (nonAdminRoles.Length > 0)
                {
                    var removeRolesResult = await userManager.RemoveFromRolesAsync(adminUser, nonAdminRoles);
                    if (!removeRolesResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Không thể loại role cũ khỏi Admin bootstrap '{bootstrapAdminEmail}': " +
                            string.Join("; ", removeRolesResult.Errors.Select(error => error.Description)));
                    }
                }

                var roleResult = currentBootstrapRoles.Contains("Admin")
                    ? IdentityResult.Success
                    : await userManager.AddToRoleAsync(adminUser, "Admin");
                if (roleResult.Succeeded)
                {
                    app.Logger.LogInformation("Đã bootstrap tài khoản Admin {Email}.", bootstrapAdminEmail);
                }
                else
                {
                    app.Logger.LogError(
                        "Không thể gán role Admin cho {Email}: {Errors}",
                        bootstrapAdminEmail,
                        string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}

app.Run();

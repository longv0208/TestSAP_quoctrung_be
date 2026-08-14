using System.Text;
using FURPMS.API.Middleware;
using FURPMS.Application.Settings;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FURPMS API",
        Version = "v1",
        Description = "FPT University Research Project Management System — Phase 0"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
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
    });

builder.Services.AddAuthorization();

// CORS: đọc danh sách origin từ config "Cors:AllowedOrigins" (mảng).
// Nếu KHÔNG cấu hình → cho phép mọi origin. API dùng Bearer token (không dùng cookie)
// nên AllowAnyOrigin an toàn — fix lỗi preflight khi FE chạy ở origin khác localhost:5173.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();
        if (corsOrigins is { Length: > 0 })
            policy.WithOrigins(corsOrigins);
        else
            policy.AllowAnyOrigin();
    });
});

var app = builder.Build();

// Tự soi cấu hình và kêu to những thứ chỉ hỏng trên máy chủ (link email trỏ localhost, thiếu
// Cloudinary nên tệp bay mỗi lần redeploy, khoá JWT vẫn là khoá mẫu trong repo…). Không cái nào
// báo lỗi khi chạy — chúng chỉ cho ra kết quả sai một cách im lặng.
FURPMS.API.Startup.ProductionReadinessCheck.Run(app);

// Migration + seed, CÓ THỬ LẠI. Mạng nội bộ của Railway mất vài giây mới sẵn sàng sau khi
// container khởi động; gọi ngay dòng đầu là DNS chưa phân giải được ⇒ tiến trình chết ⇒
// Railway đánh dấu Crashed, nhìn log thì tưởng sai cấu hình.
await FURPMS.API.Startup.DatabaseStartup.MigrateAndSeedAsync(app);

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FURPMS API v1"));

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

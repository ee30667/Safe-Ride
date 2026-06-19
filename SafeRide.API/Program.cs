using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SafeRide.API.Data;
using SafeRide.API.Interfaces;
using SafeRide.API.Repositories;
using SafeRide.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<IRideRepository, RideRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRideService, RideService>();


builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IRatingService, RatingService>();

builder.Services.AddScoped<IDriverLocationRepository, DriverLocationRepository>();
builder.Services.AddScoped<IFavoriteLocationRepository, FavoriteLocationRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<ISosRepository, SosRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IEarningsRepository, EarningsRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();

builder.Services.AddScoped<IDriverLocationService, DriverLocationService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISosService, SosService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IEarningsService, EarningsService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// JWT Authentication (replacing Cookie auth)
//var jwtKey = builder.Configuration["Jwt:Key"] ?? "SafeRideSecretKey2025MustBe32CharsLong!";
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuerSigningKey = true,
//            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
//            ValidateIssuer = false,
//            ValidateAudience = false,
//            ClockSkew = TimeSpan.Zero
//        };
//    });


// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = "SafeRideSuperSecretJwtKey2026VeryLongAndSecure123";
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://safe-ride-sigma.vercel.app"
)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    var pending = db.Database.GetPendingMigrations();
//    if (pending.Any())
//        db.Database.Migrate();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

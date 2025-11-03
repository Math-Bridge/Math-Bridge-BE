using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using MathBridgeSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using MathBridgeSystem.Infrastructure.Services;
var builder = WebApplication.CreateBuilder(args);

// Initialize Firebase with error handling
var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"];
if (string.IsNullOrEmpty(firebaseCredentialsPath) || !File.Exists(firebaseCredentialsPath))
{
    throw new FileNotFoundException("Firebase credentials file not found or path is invalid.", firebaseCredentialsPath);
}

try
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredentialsPath),
        ProjectId = builder.Configuration["Firebase:ProjectId"]
    });
}
catch (Exception ex)
{
    throw new Exception("Failed to initialize Firebase: " + ex.Message, ex);
}

// Load SMTP configuration from external JSON file with error handling
var smtpConfigPath = builder.Configuration["SmtpConfigPath"];
if (!string.IsNullOrEmpty(smtpConfigPath) && File.Exists(smtpConfigPath))
{
    builder.Configuration.AddJsonFile(smtpConfigPath, optional: false, reloadOnChange: true);
}
else
{
    Console.WriteLine($"Warning: SMTP config file not found at {smtpConfigPath}. Email functionality will be unavailable.");
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null); // Disable camelCase by default

// Database configuration with retry policy
builder.Services.AddDbContext<MathBridgeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: new List<int> { 10054, 10053, 1205 }
        )));

// HTTP Client for external services
builder.Services.AddHttpClient<IGoogleMapsService, GoogleMapsService>();

// === REPOSITORY REGISTRATIONS ===
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
builder.Services.AddScoped<ISePayRepository, SePayRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<IChildRepository, ChildRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<ICenterRepository, CenterRepository>();
builder.Services.AddScoped<ITutorCenterRepository, TutorCenterRepository>();
builder.Services.AddScoped<ITutorScheduleRepository, TutorScheduleRepository>();
builder.Services.AddScoped<ISchoolRepository, SchoolRepository>();
builder.Services.AddScoped<ICurriculumRepository, CurriculumRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
$1
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
builder.Services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();


// === SERVICE REGISTRATIONS ===
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<ISePayService, SePayService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITutorScheduleService, TutorScheduleService>();
$1

// === NOTIFICATION SERVICES ===
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISessionReminderService, SessionReminderService>();
builder.Services.AddSingleton<NotificationConnectionManager>();
builder.Services.AddScoped<IPubSubNotificationProvider, GooglePubSubNotificationProvider>();
builder.Services.AddScoped<PubSubSubscriberService>();


// === CORE BUSINESS SERVICES ===
builder.Services.AddScoped<IChildService, ChildService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<ICenterService, CenterService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<ICurriculumService, CurriculumService>();

// === INFRASTRUCTURE SERVICES ===
builder.Services.AddMemoryCache();

// === AUTHENTICATION ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ClockSkew = TimeSpan.Zero, // Disable clock skew to enforce exact token expiration
            // Map JWT claims to ClaimTypes
            NameClaimType = "sub", // Maps "sub" to ClaimTypes.NameIdentifier
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
    });

builder.Services.AddAuthorization();

// === CORS ===
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://web.vibe88.tech", "https://api.vibe88.tech", "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


// === SWAGGER ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MathBridge API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MathBridge API v1");
        c.EnableDeepLinking();
    });
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
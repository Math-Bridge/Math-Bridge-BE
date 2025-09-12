using MathBridge.Infrastructure.Data;
using MathBridge.Infrastructure.Repositories;
using MathBridge.Application.Services;
using MathBridge.Application.Interfaces;
using MathBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;

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

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null); // Disable camelCase by default

builder.Services.AddDbContext<MathBridgeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.EnableRetryOnFailure(maxRetryCount: 5,
                                   maxRetryDelay: TimeSpan.FromSeconds(10),
                                   errorNumbersToAdd: new List<int> { 10054, 10053, 1205 })));

// Repository registrations
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
builder.Services.AddScoped<ISePayRepository, SePayRepository>();

// Service registrations
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<ISePayService, SePayService>();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IEmailService, EmailService>();

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

// Add Swagger services with JWT support
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
    app.UseDeveloperExceptionPage(); // Show detailed errors in Development
    app.UseSwagger(); // Enable Swagger middleware
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MathBridge API v1");
        c.EnableDeepLinking();
    });
}

app.UseHttpsRedirection(); // Enforce HTTPS
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
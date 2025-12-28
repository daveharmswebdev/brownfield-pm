using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PropertyManager.Api.Middleware;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Infrastructure.Email;
using PropertyManager.Infrastructure.Identity;
using PropertyManager.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "PropertyManager")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Configure EF Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Database=propertymanager;Username=postgres;Password=localdev";

// Convert Render's PostgreSQL URI format to .NET format if needed
// Render provides: postgres://user:password@host:port/database
// Npgsql expects: Host=host;Port=port;Database=database;Username=user;Password=password
connectionString = ConvertPostgresConnectionString(connectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register Application layer interfaces
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Configure Email settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(EmailSettings.SectionName));

// Configure JWT settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IJwtService, JwtService>();

// Configure JWT Bearer Authentication (AC4.2)
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero  // No clock skew tolerance per Architecture doc
    };
});

builder.Services.AddAuthorization();

// Configure MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(PropertyManager.Application.Auth.RegisterCommand).Assembly);
});

// Configure FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(PropertyManager.Application.Auth.RegisterCommand).Assembly);

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // Password requirements per AC3.2
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = true;

    // Lockout settings (optional, for security)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure token lifespan for email verification (24 hours per AC3.4)
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24);
});

// Configure NSwag/OpenAPI
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "Property Manager API";
    config.Version = "v1";
    config.Description = "API for managing rental property expenses and generating tax reports";
});

var app = builder.Build();

// Register global exception handler as first middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/v1/swagger.json";
    });
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply database migrations in production
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Log.Information("Applying database migrations...");
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to apply database migrations - application cannot start");
        throw; // Prevent app from starting with failed migrations
    }
}

app.Run();

// Helper function to convert PostgreSQL connection string formats
static string ConvertPostgresConnectionString(string connectionString)
{
    // If it's already in .NET format (contains "Host="), return as-is
    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    // Parse Render's URI format: postgres://user:password@host[:port]/database
    // Port is optional - Render often omits it
    var pattern = @"^postgres(?:ql)?://([^:]+):([^@]+)@([^:/]+)(?::(\d+))?/(.+)$";
    var match = Regex.Match(connectionString, pattern);

    if (match.Success)
    {
        var user = match.Groups[1].Value;
        var password = match.Groups[2].Value;
        var host = match.Groups[3].Value;
        var port = match.Groups[4].Success ? match.Groups[4].Value : "5432";
        var database = match.Groups[5].Value;

        return $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }

    // If pattern doesn't match, return original and let it fail with a clear error
    return connectionString;
}

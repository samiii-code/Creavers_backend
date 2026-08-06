using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Creavers.API.Configurations;
using Creavers.API.Data;
using Creavers.API.Interfaces;
using Creavers.API.Middlewares;
using Creavers.API.Models;
using Creavers.API.Repositories;
using Creavers.API.Services;

// ─── 1. Bootstrap Serilog ────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Creavers API application...");

    var builder = WebApplication.CreateBuilder(args);

    // ─── 2. Serilog Host ─────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // ─── 3. Bind Configuration ───────────────────────────────────────────────
    var jwtSettings = new JwtSettings();
    builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

    // ─── 4. PostgreSQL / EF Core ─────────────────────────────────────────────
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    // ─── 5. ASP.NET Identity ─────────────────────────────────────────────────
    builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        // Password policy
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // ─── 6. JWT Authentication ───────────────────────────────────────────────
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer           = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidateAudience         = true,
            ValidAudience            = jwtSettings.Audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    // ─── 7. AutoMapper ───────────────────────────────────────────────────────
    builder.Services.AddAutoMapper(typeof(Program).Assembly);

    // ─── 8. FluentValidation ─────────────────────────────────────────────────
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    // ─── 9. Controllers ──────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ─── 10. Swagger / OpenAPI ───────────────────────────────────────────────
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "Creavers Local Services Marketplace API",
            Version     = "v1",
            Description = "ASP.NET Core 8 Web API – Week 2 (OTP) & Week 3 (Customer Task Module)"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name        = "Authorization",
            Type        = SecuritySchemeType.ApiKey,
            Scheme      = "Bearer",
            BearerFormat = "JWT",
            In          = ParameterLocation.Header,
            Description = "Enter 'Bearer' [space] and then your valid JWT token.\r\n\r\nExample: \"Bearer eyJhbGci...\""
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ─── 11. CORS ────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowConfiguredOrigins", policy =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }
        });
    });

    // ─── 12. Dependency Injection ────────────────────────────────────────────
    // Generic repository
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

    // Domain services
    builder.Services.AddScoped<IHealthService,   HealthService>();
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IAuthService,     AuthService>();
    builder.Services.AddScoped<IUserService,     UserService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IProviderService, ProviderService>();
    builder.Services.AddScoped<IAdminService,    AdminService>();
    builder.Services.AddScoped<IOtpService,     OtpService>();
    builder.Services.AddScoped<ITaskService,    TaskService>();

    // ─── Build ────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ─── 13. Seed Roles & Default Admin ──────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        string[] roles  = { "ADMIN", "PROVIDER", "CUSTOMER" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                Log.Information("Role seeded: {Role}", role);
            }
        }
    }

    // ─── 14. Middleware Pipeline ──────────────────────────────────────────────
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    // Swagger UI is served at the root (http://localhost:5000/) in all environments
    // so the API is browsable regardless of how it is launched.
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Creavers API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root (http://localhost:5000/)
    });

    app.UseCors("AllowConfiguredOrigins");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("Creavers API application starting host...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Creavers API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

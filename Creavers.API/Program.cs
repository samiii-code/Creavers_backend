using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Creavers.API.Configurations;
using Creavers.API.Data;
using Creavers.API.Interfaces;
using Creavers.API.Middlewares;
using Creavers.API.Repositories;
using Creavers.API.Services;

// 1. Configure Serilog Bootstrap Logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Creavers API application...");

    var builder = WebApplication.CreateBuilder(args);

    // 2. Add Serilog to Host
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // 3. Bind Configurations
    var jwtSettings = new JwtSettings();
    builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

    // 4. Configure PostgreSQL EF Core DbContext
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    // 5. Register AutoMapper
    builder.Services.AddAutoMapper(typeof(Program).Assembly);

    // 6. Register FluentValidation
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    // 7. Register JWT Authentication (Configuration Only)
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    // 8. Register Controllers (Using Controllers - No Minimal APIs)
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // 9. Configure Swagger / OpenAPI with JWT Authorization Bearer Scheme
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Creavers Local Services Marketplace API",
            Version = "v1",
            Description = "ASP.NET Core 8 Web API Foundation for Creavers Local Services Marketplace"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter 'Bearer' [space] and then your valid JWT token.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
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
                Array.Empty<string>()
            }
        });
    });

    // 10. Configure CORS Policy
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowConfiguredOrigins", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
    });

    // 11. Register Dependency Injection (Repositories & Services)
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IHealthService, HealthService>();

    var app = builder.Build();

    // 12. Middleware Pipeline Execution
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Creavers API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();

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

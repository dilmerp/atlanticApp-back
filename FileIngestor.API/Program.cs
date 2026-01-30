using Common.Domain.Interfaces;
using Common.Persistence;
using Common.Persistence.Data;
using Common.Persistence.Repositories;
using FileIngestor.API.Customizations.Swagger;
using FileIngestor.API.Extensions;
using FileIngestor.API.Filters;
using FileIngestor.API.Middleware;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using System;
using System.Text.Json.Serialization;
using MediatR;
using FileIngestor.Application;
using FileIngestor.Application.Interfaces;
using FileIngestor.Infrastructure.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    const string MiPoliticaCORS = "MiPoliticaCORS";

    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;

    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext());

    Log.Information("Starting FileIngestor.API host (Control Service)...");

    // --- 1. CONFIGURACIÓN DE CORS ---
    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                         ?? new[] { "http://localhost:4200" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MiPoliticaCORS, policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // --- 2. SERVICIOS DE INFRAESTRUCTURA ---
    builder.Services.AddRateLimiterServices(configuration);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
    );

    builder.Services.AddScoped<IApplicationDbContext>(
        provider => provider.GetRequiredService<AppDbContext>()
    );

    builder.Services.AddTransient<IJobStatusRepository, JobStatusRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    });

    builder.Services.AddTransient<IFileUploadService, SeaweedFsStorageService>();
    builder.Services.AddHttpClient();
    builder.Services.AddCoreServices(configuration);
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddJwtAuthentication(configuration);
    builder.Services.AddAuthorizationPolicies();

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidateModelAttribute>();
    }).AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddHealthChecks()
        .AddCheck("Self", () => HealthCheckResult.Healthy("FileIngestor running."), tags: new[] { "live" });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSignalR();

    // --- CONFIGURACIÓN DE REDIS CORREGIDA ---
    var redisConn = configuration.GetConnectionString("RedisConnection")
                    ?? configuration["Redis:RedisConnection"]
                    ?? "redis:6379,password=Peru2412,abortConnect=false"; 

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = "AtlanticApp:"; 
    });

    Log.Information("Servicios de Redis (IDistributedCache) configurados correctamente con InstanceName: AtlanticApp:");

    var app = builder.Build();

    // --- 3. PIPELINE DE MIDDLEWARES 

    // Manejo global de excepciones
    app.UseMiddleware<ExceptionMiddleware>();

    // Extensiones personalizadas (Logs de Serilog, etc.)
    app.UsePipelineExtensions(app.Environment);

    // Enrutamiento base
    app.UseRouting();

    // CORS: Debe ir SIEMPRE después de Routing y antes de Auth
    app.UseCors(MiPoliticaCORS);

    // Limitador de peticiones y Seguridad
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // --- 4. MAPEO DE ENDPOINTS ---

    app.MapControllers();
    app.UseSignalRHubs();

    // Health Checks
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // --- BLOQUE PARA MIGRACIONES AUTOMÁTICAS ---
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            Log.Information("Aplicando migraciones en la base de datos...");
            context.Database.Migrate();
            Log.Information("Migraciones aplicadas con éxito.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ocurrió un error al aplicar las migraciones.");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
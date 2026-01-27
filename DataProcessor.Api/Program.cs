using Common.Domain.Interfaces;
using Common.Persistence;
using Common.Persistence.Data;
using HealthChecks.UI.Client;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;
using DataProcessor.Application.Features.CargaMasiva.Commands;
using DataProcessor.Application.Features.CargaMasiva.Queries;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    const string MiPoliticaCORS = "AtlanticCityPolicy";
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;

    // 1. Serilog
    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext());

    Log.Information("Starting DataProcessor.API host...");

    // 2. Redis Configuration
    var redisSection = configuration.GetSection("Redis");
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        // options.Configuration = redisSection["RedisConnection"];
        // options.InstanceName = redisSection["InstanceName"];
        options.Configuration = "redis_cache:6379,password=Peru2412,abortConnect=false";
        options.InstanceName = "AtlanticApp:"; // <--- Unificado
    });

    // 3. MediatR - Registro consolidado
    builder.Services.AddMediatR(cfg => {
        cfg.RegisterServicesFromAssembly(typeof(GetCargaStatusQuery).Assembly);
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    });

    // 4. Database (PostgreSQL)
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
    );

    builder.Services.AddScoped<IApplicationDbContext>(
        provider => provider.GetRequiredService<AppDbContext>()
    );

    // 5. Infrastructure & Services
    builder.Services.AddSingleton<FileIngestor.Application.Interfaces.IMessagePublisher, DataProcessor.Worker.Workers.Services.RabbitMqPublisher>();
    builder.Services.AddHttpClient();
    builder.Services.AddEndpointsApiExplorer();

    // 6. Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "DataProcessor API", Version = "v1" });
    });

    // 7. Controllers & JSON
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    // 8. CORS Config - CORREGIDO
    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };

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

    // 9. Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck("Self", () => HealthCheckResult.Healthy("DataProcessor running."), tags: new[] { "live" });

    var app = builder.Build();

    // --- PIPELINE DE MIDDLEWARE ---

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DataProcessor API V1");
        c.RoutePrefix = "swagger";
    });

    app.UseRouting();
    app.UseCors(MiPoliticaCORS);
    app.UseAuthorization();
    
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapControllers();

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
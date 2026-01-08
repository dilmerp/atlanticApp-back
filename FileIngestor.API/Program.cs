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
using Npgsql.EntityFrameworkCore.PostgreSQL;
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

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services)
                     .Enrich.FromLogContext(),
        preserveStaticLogger: false);

    Log.Information("Starting FileIngestor.API host...");

    builder.Services.AddRateLimiterServices(configuration);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
    );

    builder.Services.AddScoped<Common.Domain.Interfaces.IApplicationDbContext>(
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

    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
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

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck("Self", () => HealthCheckResult.Healthy("FileIngestor running."), tags: new[] { "live" });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSignalR();

    var app = builder.Build();

    // -----------------------------------------------------
    // Pipeline
    // -----------------------------------------------------
    app.UseMiddleware<ExceptionMiddleware>();

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

    app.UsePipelineExtensions(app.Environment);
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseCors(MiPoliticaCORS);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.UseSignalRHubs();

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
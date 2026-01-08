using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using System;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;


namespace FileIngestor.Infrastructure
{
    public static class RedisConfigurationExtensions
    {
        // Método interno que será llamado por AddInfrastructure
        public static IServiceCollection AddRedisServices(this IServiceCollection services, IConfiguration configuration)
        {
            var redisSection = configuration.GetSection("Redis");
            var redisConnectionFromSection = redisSection.GetValue<string?>("RedisConnection");
            var redisConnectionFromConnStrings = configuration.GetConnectionString("RedisConnection");

            // Lógica para obtener la cadena de conexión
            var redisConn = !string.IsNullOrWhiteSpace(redisConnectionFromSection)
                ? redisConnectionFromSection
                : redisConnectionFromConnStrings;

            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                try
                {
                    // Configuración Detallada de ConnectionMultiplexer (Cliente de bajo nivel)
                    var options = ConfigurationOptions.Parse(redisConn, true);
                    options.AbortOnConnectFail = redisSection.GetValue<bool?>("AbortOnConnectFail") ?? options.AbortOnConnectFail;
                    options.ConnectRetry = redisSection.GetValue<int?>("ConnectRetry") ?? options.ConnectRetry;
                    options.ConnectTimeout = redisSection.GetValue<int?>("ConnectTimeout") ?? 15000;
                    options.SyncTimeout = redisSection.GetValue<int?>("SyncTimeout") ?? options.SyncTimeout;
                    options.KeepAlive = redisSection.GetValue<int?>("KeepAlive") ?? options.KeepAlive;

                    // Conexión síncrona/asíncrona controlada en el startup
                    var muxer = ConnectionMultiplexer.ConnectAsync(options).GetAwaiter().GetResult();

                    // Suscribirse a eventos para observabilidad
                    muxer.ConnectionFailed += (sender, evt) =>
                        Log.Warning("Redis ConnectionFailed - EndPoint: {EndPoint}, FailureType: {FailureType}, Exception: {Exception}", evt.EndPoint, evt.FailureType, evt.Exception?.Message);
                    muxer.ConnectionRestored += (sender, evt) =>
                        Log.Information("Redis ConnectionRestored - EndPoint: {EndPoint}", evt.EndPoint);
                    muxer.ErrorMessage += (sender, evt) =>
                        Log.Error("Redis ErrorMessage: {Message}", evt.Message);
                    muxer.ConfigurationChanged += (sender, evt) =>
                        Log.Information("Redis ConfigurationChanged: {EndPoint}", evt.EndPoint);

                    services.AddStackExchangeRedisCache(opt =>
                    {
                        opt.Configuration = redisConn;
                        opt.InstanceName = "ComercialApp_";
                    });

                    
                    services.AddSingleton<IConnectionMultiplexer>(muxer);

                    
                    var healthTimeoutSeconds = redisSection.GetValue<int?>("HealthCheckTimeoutSeconds") ?? 3;
                    services.AddHealthChecks()
                            .AddRedis(redisConn, name: "RedisCacheServiceCheck", timeout: TimeSpan.FromSeconds(healthTimeoutSeconds), tags: new[] { "cache", "ready" });

                    Log.Information(" Servicios de Redis (IDistributedCache, IConnectionMultiplexer, HealthCheck) configurados correctamente.");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, " No se pudo conectar a Redis en el arranque. El caché Redis NO estará disponible.");
                }
            }
            else
            {
                Log.Warning("⚠ No se encontró la cadena de conexión a Redis. El caché distribuido NO estará disponible.");
            }
            return services;
        }
    }
}
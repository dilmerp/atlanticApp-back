using FileIngestor.Application.Interfaces;
using FileIngestor.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FileIngestor.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // --- Servicios de Infraestructura ---
            services.AddRedisServices(configuration); 
            services.AddMemoryCache();
            services.AddTransient<ICacheService, MemoryCacheService>();
            services.AddTransient<IDistributedCache, MemoryDistributedCache>(); 

            return services;
        }
    }
}

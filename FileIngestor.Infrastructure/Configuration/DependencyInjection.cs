using FileIngestor.Application.Interfaces;
using FileIngestor.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FileIngestor.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            
            services.AddHttpClient<IFileUploadService, SeaweedFsStorageService>(client =>
            {
            
            })
            
            .SetHandlerLifetime(TimeSpan.FromMinutes(5)); 

            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

            return services;
        }
    }
}
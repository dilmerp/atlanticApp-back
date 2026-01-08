using FileIngestor.Application.Behaviors;
using FileIngestor.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration; 
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FileIngestor.Application.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Registro de MediatR (handlers)
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // 2. AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // 3. FluentValidation (registra validadores)
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // 4. Registrar manualmente los Pipeline Behaviors (evita duplicidad en Program.cs)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehaviorValue<,>));
            services.AddTransient(typeof(IPipelineBehavior<ICommandVoid, Unit>), typeof(TransactionBehaviorVoid<ICommandVoid>));

            return services;
        }
    }
}

using Common.Domain.Interfaces;
using Common.Persistence.Data;
using Common.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Persistence
{
    /// <summary>
    /// Clase de extensión para configurar y registrar los servicios de la capa de Persistencia
    /// en el contenedor de Inversión de Control (IoC).
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped(typeof(IRepositoryAsync<>), typeof(RepositoryAsync<>));
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddHealthChecks()
                    .AddNpgSql(
                        connectionString: connectionString,
                        name: "PostgreSQL-DB-Check", 
                        tags: new[] { "db", "ready" } 
                    );
            }

            return services;
        }
    }
}
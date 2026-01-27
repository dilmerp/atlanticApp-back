// Usings específicos de tu arquitectura
using FileIngestor.API.Customizations.Swagger;
using FileIngestor.Application.Configuration; // Para AddApplication
using FileIngestor.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;
using System.Threading.RateLimiting;
using FileIngestor.API.Authorizations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using FileIngestor.Application.Interfaces; 
using FileIngestor.Infrastructure.Services;

namespace FileIngestor.API.Extensions
{
    public static class ServicesCollectionExtensions
    {
        // 1. Configuración de servicios Core, Application e Infrastructure
        public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplication(configuration);
            services.AddInfrastructure(configuration);

            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
            services.AddControllers();
            services.AddEndpointsApiExplorer();

            return services;
        }

        // 2. Configuración de Swagger/OpenAPI
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileIngestor API", Version = "v1" });

                // Definición de seguridad para JWT
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Ingrese 'Bearer' [espacio] y luego su token en el campo de texto a continuación."
                });

                // Requisito de seguridad para los endpoints
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
                        new string[] {}
                    }
                });

                c.OperationFilter<AuthorizeOperationFilter>();
            });
            return services;
        }

        // 3. Configuración de Autenticación JWT
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"] ?? throw new ArgumentNullException("JwtSettings:Secret no puede ser nulo.");
            var issuer = jwtSettings["Issuer"] ?? throw new ArgumentNullException("JwtSettings:Issuer no puede ser nulo.");
            var audience = jwtSettings["Audience"] ?? throw new ArgumentNullException("JwtSettings:Audience no puede ser nulo.");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });

            return services;
        }

        // 4. Configuración de Autorización (Políticas)
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationHandler, Authorizations.RoleBasedAuthorizationHandler>();
            
            services.AddAuthorization(options =>
            {
                options.AddPolicy("MustBeAdmin", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("MustBeProcessor", policy =>
                    policy.RequireClaim("Scope", "FileProcessor"));
            });

            return services;
        }

        public static IServiceCollection AddRateLimiterServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Se implementa el Limitador Global
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var userIdentifier = httpContext.User.Identity?.IsAuthenticated == true
                        ? httpContext.User.FindFirst("email")?.Value ?? httpContext.User.Identity.Name!
                        : httpContext.Connection.RemoteIpAddress?.ToString() ?? Guid.NewGuid().ToString();

                    return RateLimitPartition.GetFixedWindowLimiter(
                        userIdentifier,
                        factory: (key) => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromSeconds(10)
                        });
                });
            });

            return services;
        }
    }
}
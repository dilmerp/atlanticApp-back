using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using FileIngestor.API.Middleware;
using Serilog;

namespace FileIngestor.API.Extensions
{
    /// <summary>
    /// Extensiones para la configuración del pipeline de middlewares (IApplicationBuilder).
    /// Centraliza la configuración de middlewares como el manejo de errores global y Swagger.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UsePipelineExtensions(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseSerilogRequestLogging();
            
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

           // app.UseHttpsRedirection();
           // app.UseAuthorization();

            return app;
        }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileIngestor.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        private readonly IHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        
        public ExceptionMiddleware(
                RequestDelegate next,
                IHostEnvironment env,
                IHttpContextAccessor httpContextAccessor)
        {
            _next = next;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _logger = Log.ForContext<ExceptionMiddleware>();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // 1. Loguear la Excepción
            _logger.Error(exception, "Ocurrió un error inesperado procesando la solicitud en el middleware.");

            // 2. Configurar la Respuesta HTTP
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                Mensaje = "Se produjo un error interno en el servidor. Intente nuevamente más tarde.",
                StatusCode = context.Response.StatusCode,
                Detalle = context.Response.StatusCode == (int)HttpStatusCode.InternalServerError ?
                          exception.Message : null
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}

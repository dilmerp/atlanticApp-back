using FileIngestor.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileIngestor.API.Middleware
{
    /// <summary>
    /// Middleware para capturar excepciones no controladas a nivel global.
    /// Maneja ValidationException (400) y errores genéricos (500),
    /// devolviendo respuestas JSON consistentes.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ExceptionResponse();

            if (exception is ValidationException validationException)
            {
                // Manejo específico para errores de validación
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Se encontraron errores de validación en la solicitud.";
                response.Errors = validationException.Errors;
            }
            else
            {
                // Manejo para errores genéricos
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "Ocurrió un error interno del servidor. Consulte los logs para más detalles.";

                // Registrar el error completo
                Log.Error(exception, "Error de Servidor no manejado. Path: {Path}", context.Request.Path);

                // 👉 En entorno Development, incluir detalles en la respuesta
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    response.Errors = new
                    {
                        exception.Message,
                        exception.StackTrace
                    };
                }
            }

            var jsonResponse = JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return context.Response.WriteAsync(jsonResponse);
        }

        // Clase interna para estandarizar la respuesta de error enviada al cliente
        private class ExceptionResponse
        {
            public int StatusCode { get; set; }
            public string Message { get; set; } = "Ocurrió un error interno del servidor. Intente nuevamente más tarde.";
            public object? Errors { get; set; }
        }
    }
}
using Microsoft.AspNetCore.Builder;
using FileIngestor.API.Middleware;

namespace FileIngestor.API.Middleware
{
    public static class ExceptionMiddlewareExtensions
    {
        
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
        
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
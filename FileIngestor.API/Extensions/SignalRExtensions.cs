using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FileIngestor.API.Extensions
{
    public static class SignalRExtensions
    {
        public static IEndpointRouteBuilder UseSignalRHubs(this IEndpointRouteBuilder app)
        {
            return app;
        }
    }
}

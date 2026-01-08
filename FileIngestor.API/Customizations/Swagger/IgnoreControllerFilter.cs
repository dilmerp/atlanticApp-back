using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace FileIngestor.API.Customizations.Swagger
{
    public class IgnoreControllerFilter : IDocumentFilter
    {
        private const string ControllerNameToIgnore = "CargaMasiva";

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var pathsToRemove = swaggerDoc.Paths
                .Where(pathItem => pathItem.Value.Operations
                    .Any(op =>
                    {
                        var controllerActionDescriptor = context.ApiDescriptions
                            .FirstOrDefault(api => api.RelativePath?.Trim('/') == pathItem.Key.Trim('/'))?
                            .ActionDescriptor as ControllerActionDescriptor;

                        return controllerActionDescriptor?.ControllerName == ControllerNameToIgnore; 
                    }))
                .Select(pathItem => pathItem.Key)
                .ToList();

            foreach (var path in pathsToRemove)
            {
                swaggerDoc.Paths.Remove(path);
            }
        }
    }
}
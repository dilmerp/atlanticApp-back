using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace FileIngestor.API.Customizations.Swagger
{
    public class SwaggerFileUploadFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasUploadDto = context.MethodInfo
                .GetParameters()
                .Any(p => p.ParameterType == typeof(FileIngestor.Application.DTO.UploadFileDto));

            if (hasUploadDto)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content =
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties =
                                {
                                    ["file"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    },
                                    ["periodo"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "Periodo en formato AAAAMM (ej. 202601)"
                                    }
                                },
                                Required = new HashSet<string> { "file", "periodo" }

                            }
                        }
                    }
                };
            }
        }
    }
}
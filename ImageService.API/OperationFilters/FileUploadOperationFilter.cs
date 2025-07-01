using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ImageService.API.OperationFilters;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasFileUpload = context.MethodInfo.GetParameters()
            .Any(p => p.ParameterType == typeof(IFormFile) ||
                      p.ParameterType.GetProperties().Any(prop => prop.PropertyType == typeof(IFormFile)));

        if (!hasFileUpload)
            return;

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
                            ["image"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary"
                            }
                        },
                        Required = new HashSet<string> { "image" }
                    }
                }
            }
        };
    }
}
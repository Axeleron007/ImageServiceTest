using ImageService.Core.Dtos;
using ImageService.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageService.API.Middleware;

public class ErrorHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (BusinessValidationException ex)
        {
            _logger.LogError(ex, "BusinessValidationException occurred");

            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

            var response = new ErrorResponseDto
            {
                Error = "Business validation failed.",
                Details = ex.Message
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
        catch (TargetHeightExceededException ex)
        {
            _logger.LogError(ex, "TargetHeightExceededException occurred");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var response = new ErrorResponseDto
            {
                Error = "Error occurred.",
                Details = ex.Message
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new ErrorResponseDto
            {
                Error = "An unexpected error occurred.",
                Details = ex.Message
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}

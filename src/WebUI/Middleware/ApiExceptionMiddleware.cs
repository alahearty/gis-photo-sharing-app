using System.Text.Json;
using gis_photo_sharing_app.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace gis_photo_sharing_app.WebUI.Middleware;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var problem = new ProblemDetails
        {
            Instance = context.Request.Path,
            Title = "An error occurred while processing the request.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected error occurred. Please try again later.",
        };

        switch (exception)
        {
            case ValidationException validation:
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Title = "Validation failed.";
                problem.Detail = "One or more fields are invalid.";
                problem.Extensions["errors"] = validation.Errors;
                break;

            case InvalidImageException image:
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Title = "Invalid image.";
                problem.Detail = image.Message;
                break;

            case BadHttpRequestException badRequest:
                problem.Status = badRequest.StatusCode;
                problem.Title = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "The uploaded file is too large."
                    : "The request could not be processed.";
                problem.Detail = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "Reduce the image size and try again."
                    : "Check the submitted values and try again.";
                break;

            default:
                _logger.LogError(exception, "Unhandled API exception for {Method} {Path}.",
                    context.Request.Method, context.Request.Path);
                break;
        }

        if (problem.Status != StatusCodes.Status500InternalServerError)
        {
            _logger.LogWarning(exception, "Request failed with status {Status} for {Method} {Path}.",
                problem.Status, context.Request.Method, context.Request.Path);
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        await context.Response.WriteAsync(json);
    }
}

using System.Net;
using Dappi.HeadlessCms.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ArgumentNullException or ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException or PropertyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            MethodNotAllowedException => StatusCodes.Status405MethodNotAllowed,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        
        logger.LogError(exception, "{ExceptionType} occurred", exception.GetType().Name);

        var problemDetails = CreateProblemDetails(exception, context, statusCode);
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static ProblemDetails CreateProblemDetails<T>(T ex, HttpContext context, int statusCode)
        where T : Exception
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        var activity = context.Features.Get<IHttpActivityFeature>()?.Activity;
        return new ProblemDetails()
        {
            Type = ex.GetType().Name,
            Title = ((HttpStatusCode)statusCode).ToString(),
            Detail = ex.Message,
            Instance = $"[{context.Request.Method}] {context.Request.Path}",
            Status = statusCode,
            Extensions =
            {
                {"requestId", context.TraceIdentifier},
                {"traceId", activity?.Id ?? string.Empty}
            }
        };
    }
}
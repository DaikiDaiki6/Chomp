using System;
using System.Net;
using System.Text.Json;
using Serilog;

namespace FinanceService.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception occurred while processing {Path}", context.Request.Path);

            var (statusCode, error) = ex switch
            {
                ArgumentException => ((int)HttpStatusCode.BadRequest, ex.Message),
                KeyNotFoundException => ((int)HttpStatusCode.NotFound, ex.Message),
                UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, ex.Message),
                _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred")
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var result = JsonSerializer.Serialize(new
            {
                error,
                detail = ex.Message
            });

            await context.Response.WriteAsync(result);
        }
    }
}
// used to make app.UseMiddleware<ExceptionHandlingMiddleware>(); to this app.UseExceptionHandling();
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}



using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.EnrollmentService.Infrastructure.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string API_KEY_HEADER = "X-Api-Key";

    public ApiKeyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/internal"))
        {
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("{\"error\":\"api_key_missing\"}");
                return;
            }

            var expectedKey = context.RequestServices.GetRequiredService<IConfiguration>()["InternalApiKeys:EnrollmentService"];
            if (!string.Equals(expectedKey, extractedApiKey))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("{\"error\":\"invalid_api_key\"}");
                return;
            }
        }
        await _next(context);
    }
}
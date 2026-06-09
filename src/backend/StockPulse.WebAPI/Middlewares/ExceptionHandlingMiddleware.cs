using System.Net;

namespace StockPulse.WebAPI.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogger<ExceptionHandlingMiddleware> logger)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            logger.LogError(ex,
                "An unhandled exception occurred. TraceId: {TraceId} | Method: {Method} | Path: {Path}{QueryString} | User: {UserId}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty,
                userId);
            await HandleExceptionAsync(context);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context)
    {
        HttpStatusCode status = HttpStatusCode.InternalServerError;
        string message = "Beklenmeyen bir hata oluştu.";

        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.StatusCode = (int)status;

        await context.Response.WriteAsync(message);
    }
}

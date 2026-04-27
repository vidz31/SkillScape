namespace SkillScape.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new { message = exception.Message, success = false };

        return exception switch
        {
            UnauthorizedAccessException => HandleUnauthorizedAsync(context, response),
            InvalidOperationException => HandleBadRequestAsync(context, response),
            _ => HandleInternalServerErrorAsync(context, response)
        };
    }

    private static Task HandleUnauthorizedAsync(HttpContext context, object response)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task HandleBadRequestAsync(HttpContext context, object response)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task HandleInternalServerErrorAsync(HttpContext context, object response)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return context.Response.WriteAsJsonAsync(response);
    }
}

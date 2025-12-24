using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace backend.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
  {
    logger.LogError(exception, "An unhandled exception has occurred: {Message}", exception.Message);

    var (status, title) = exception switch
    {
      ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
      UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
      KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
      _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
    };

    var problemDetails = new ProblemDetails
    {
      Status = status,
      Title = title,
      Detail = exception.Message,
      Instance = httpContext.Request.Path
    };

    httpContext.Response.StatusCode = status;
    await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

    return true;
  }
}

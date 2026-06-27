using FluentValidation;
using Watchtower.Domain.Exceptions;

namespace Watchtower.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        => (_next, _logger) = (next, logger);

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            await Write(ctx, 400, "Validation failed",
                ex.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
        }
        catch (ConflictException ex) { await Write(ctx, 409, ex.Message); }
        catch (NotFoundException ex) { await Write(ctx, 404, ex.Message); }
        catch (ForbiddenException ex) { await Write(ctx, 403, ex.Message); }
        catch (UnauthorizedAccessException ex) { await Write(ctx, 401, ex.Message); }
        catch (DomainException ex) { await Write(ctx, 400, ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await Write(ctx, 500, "An unexpected error occurred.");
        }
    }

    private static Task Write(HttpContext ctx, int status, string detail, string[]? errors = null)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new { status, detail, errors });
    }
}

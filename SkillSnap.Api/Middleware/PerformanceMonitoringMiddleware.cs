using System.Diagnostics;

namespace SkillSnap.Api.Middleware;

/// <summary>
/// Middleware for monitoring API endpoint performance and detecting slow queries.
/// Logs all request timings and warns about requests exceeding the slow query threshold.
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly long _slowQueryThresholdMs;
    private readonly bool _logNormalRequests;

    // Paths to exclude from performance logging
    private static readonly string[] ExcludedPaths =
    {
        "/health",
        "/healthz",
        "/_framework/",
        "/css/",
        "/js/",
        "/favicon.ico"
    };

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        
        // Get slow query threshold from config, default to 1000ms (1 second)
        _slowQueryThresholdMs = configuration.GetValue<long>("PerformanceMonitoring:SlowQueryThresholdMs", 1000);
        
        // Get whether to log normal requests, default to true
        _logNormalRequests = configuration.GetValue<bool>("PerformanceMonitoring:LogNormalRequests", true);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip performance logging for excluded paths (health checks, static files)
        if (IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Get or generate correlation ID for request tracing
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..8]; // Use first 8 chars of GUID for brevity
        
        // Add correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            return Task.CompletedTask;
        });

        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var queryString = context.Request.QueryString.HasValue 
            ? context.Request.QueryString.Value 
            : string.Empty;
        Exception? exception = null;

        try
        {
            // Continue processing the request
            await _next(context);
        }
        catch (Exception ex)
        {
            // Capture exception for logging but re-throw to let other middleware handle it
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;
            var responseSize = context.Response.ContentLength ?? 0;
            
            // Get username after authentication middleware has processed the request
            var userName = context.User?.Identity?.Name ?? "anonymous";

            // Log exceptions with performance context
            if (exception != null)
            {
                _logger.LogError(exception,
                    "REQUEST FAILED [{CorrelationId}]: {Method} {Path}{QueryString} by {User} failed after {ElapsedMs}ms with exception {ExceptionType} - {ExceptionMessage}",
                    correlationId,
                    requestMethod,
                    requestPath,
                    queryString,
                    userName,
                    elapsedMs,
                    exception.GetType().Name,
                    exception.Message);
            }
            // Log performance metrics for successful requests
            else if (elapsedMs >= _slowQueryThresholdMs)
            {
                // Slow query detected - log as warning with all context
                _logger.LogWarning(
                    "SLOW REQUEST [{CorrelationId}]: {Method} {Path}{QueryString} by {User} completed in {ElapsedMs}ms with status {StatusCode}, size: {ResponseSize} bytes (threshold: {ThresholdMs}ms)",
                    correlationId,
                    requestMethod,
                    requestPath,
                    queryString,
                    userName,
                    elapsedMs,
                    statusCode,
                    responseSize,
                    _slowQueryThresholdMs);
            }
            else if (_logNormalRequests)
            {
                // Normal request - log as information (only if enabled)
                _logger.LogInformation(
                    "Request [{CorrelationId}]: {Method} {Path} by {User} completed in {ElapsedMs}ms with status {StatusCode}",
                    correlationId,
                    requestMethod,
                    requestPath,
                    userName,
                    elapsedMs,
                    statusCode);
            }
        }
    }

    /// <summary>
    /// Checks if the request path should be excluded from performance logging.
    /// </summary>
    private static bool IsExcludedPath(PathString path)
    {
        return ExcludedPaths.Any(excludedPath => 
            path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase));
    }
}

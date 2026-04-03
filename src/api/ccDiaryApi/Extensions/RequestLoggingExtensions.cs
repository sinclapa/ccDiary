// <copyright file="RequestLoggingExtensions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Extensions
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Builder;
    using Serilog;

    /// <summary>
    /// Request logging middleware extensions.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Middleware wiring and Serilog side effects are validated by integration behavior rather than unit coverage metrics.")]
    public static class RequestLoggingExtensions
    {
        /// <summary>
        /// Adds completion logging for traced HTTP requests.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <returns>The same <paramref name="app"/> for chaining.</returns>
        public static IApplicationBuilder UseRequestCompletionLogging(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (!OpenTelemetryExtensions.ShouldTraceRequest(context))
                {
                    await next();
                    return;
                }

                var requestStart = Stopwatch.StartNew();
                await next();
                requestStart.Stop();

                var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;
                var spanId = Activity.Current?.SpanId.ToString() ?? string.Empty;
                var statusCode = context.Response.StatusCode;
                var sanitizedMethod = context.Request.Method?.Replace("\r", string.Empty).Replace("\n", string.Empty);
                var sanitizedPath = context.Request.Path.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty);

                if (statusCode >= 500)
                {
                    Log.Logger.Warning(
                        "HTTP request completed with server error. Method={Method} Path={Path} StatusCode={StatusCode} DurationMs={DurationMs} TraceId={TraceId} SpanId={SpanId}",
                        sanitizedMethod,
                        sanitizedPath,
                        statusCode,
                        requestStart.ElapsedMilliseconds,
                        traceId,
                        spanId);
                }
                else
                {
                    Log.Logger.Information(
                        "HTTP request completed. Method={Method} Path={Path} StatusCode={StatusCode} DurationMs={DurationMs} TraceId={TraceId} SpanId={SpanId}",
                        sanitizedMethod,
                        sanitizedPath,
                        statusCode,
                        requestStart.ElapsedMilliseconds,
                        traceId,
                        spanId);
                }
            });
        }
    }
}
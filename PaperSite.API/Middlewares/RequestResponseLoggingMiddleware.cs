using Microsoft.AspNetCore.Mvc.Controllers;
using PaperSite.Application.Interfaces;
using PaperSite.Domain.Entities;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;

namespace PaperSite.API.Middlewares;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILogService logService)
    {
        var endpoint = context.GetEndpoint();
        var controllerAction = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

        if (controllerAction is null)
        {
            await _next(context);
            return;
        }

        var requestBody = await ReadRequestBodyAsync(context.Request);
        var originalResponseBody = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var responseJson = await ReadResponseBodyAsync(context.Response);
            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalResponseBody, context.RequestAborted);
            context.Response.Body = originalResponseBody;

            await SaveLogAsync(
                logService,
                context,
                controllerAction.ControllerName,
                requestBody,
                responseJson,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();

        if (request.ContentLength is null or 0)
        {
            return string.Empty;
        }

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        response.Body.Position = 0;

        return body;
    }

    private async Task SaveLogAsync(
        ILogService logService,
        HttpContext context,
        string controllerName,
        string requestBody,
        string responseJson,
        long executionTimeMs)
    {
        try
        {
            var now = DateTime.Now;
            var persianCalendar = new PersianCalendar();

            var log = new Log
            {
                controllerName = controllerName,
                requestJson = requestBody,
                responseJson = responseJson,
                persianDate = $"{persianCalendar.GetYear(now):0000}/{persianCalendar.GetMonth(now):00}/{persianCalendar.GetDayOfMonth(now):00}",
                Time = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                statusCode = context.Response.StatusCode,
                ipAddress = context.Connection.RemoteIpAddress?.ToString(),
                ExecutionTimeMs = executionTimeMs,
                userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            await logService.AddAsync(log, context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save request/response log for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
    }
}

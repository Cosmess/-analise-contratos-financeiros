using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace RagFinanceiro.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Requisicao cancelada pelo cliente. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = 499;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex,
                "Acesso nao autorizado. Path: {Path} | TraceId: {TraceId}",
                context.Request.Path,
                context.TraceIdentifier);

            await WriteErrorAsync(context, HttpStatusCode.Unauthorized, "Nao autorizado", ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex,
                "Requisicao invalida. Path: {Path} | TraceId: {TraceId}",
                context.Request.Path,
                context.TraceIdentifier);

            await WriteErrorAsync(context, HttpStatusCode.BadRequest, "Dados invalidos", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex,
                "Falha de operacao/configuracao. Path: {Path} | TraceId: {TraceId}",
                context.Request.Path,
                context.TraceIdentifier);

            await WriteErrorAsync(context, HttpStatusCode.InternalServerError,
                "Erro interno no servidor",
                "Ocorreu uma falha de configuracao ou operacao. Contate o suporte.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Erro nao tratado. Path: {Path} | Method: {Method} | TraceId: {TraceId}",
                context.Request.Path,
                context.Request.Method,
                context.TraceIdentifier);

            await WriteErrorAsync(context, HttpStatusCode.InternalServerError,
                "Erro interno no servidor",
                "Ocorreu um erro inesperado. Tente novamente ou contate o suporte.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

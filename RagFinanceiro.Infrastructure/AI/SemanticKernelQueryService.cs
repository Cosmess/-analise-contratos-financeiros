#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using RagFinanceiro.Domain.Services;

namespace RagFinanceiro.Infrastructure.AI;

public class SemanticKernelQueryService(Kernel kernel, ILogger<SemanticKernelQueryService> logger) : ILlmQueryService
{
    private const string PromptTemplate = """
        Voce e assistente juridico especializado em contratos financeiros.
        Responda APENAS com base nas clausulas abaixo.
        Se nao encontrar, responda: "Informacao nao consta no contrato."
        Cite o trecho exato que fundamenta sua resposta.

        CLAUSULAS:
        {{$context}}

        PERGUNTA: {{$question}}
        RESPOSTA:
        """;

    public async Task<string> AskAsync(string context, string question, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Enviando pergunta ao LLM. QuestionLength: {QuestionLength} | ContextLength: {ContextLength}",
            question.Length, context.Length);

        try
        {
            var fn = kernel.CreateFunctionFromPrompt(
                PromptTemplate,
                new OpenAIPromptExecutionSettings { Temperature = 0, MaxTokens = 600 }
            );

            var result = await kernel.InvokeAsync(fn, new KernelArguments
            {
                ["context"] = context,
                ["question"] = question
            }, cancellationToken);

            var answer = result.ToString();

            logger.LogInformation(
                "Resposta recebida do LLM. AnswerLength: {AnswerLength}",
                answer.Length);

            return answer;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Chamada ao LLM cancelada.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na chamada ao LLM.");
            throw;
        }
    }
}

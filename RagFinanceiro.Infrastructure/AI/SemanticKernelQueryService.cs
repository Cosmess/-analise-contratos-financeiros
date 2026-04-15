#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using RagFinanceiro.Domain.Services;

namespace RagFinanceiro.Infrastructure.AI;

public class SemanticKernelQueryService : ILlmQueryService
{
    private readonly Kernel _kernel;

    private const string PromptTemplate = """
        Você é assistente jurídico especializado em contratos financeiros.
        Responda APENAS com base nas cláusulas abaixo.
        Se não encontrar, responda: "Informação não consta no contrato."
        Cite o trecho exato que fundamenta sua resposta.

        CLÁUSULAS:
        {{$context}}

        PERGUNTA: {{$question}}
        RESPOSTA:
        """;

    public SemanticKernelQueryService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> AskAsync(string context, string question, CancellationToken cancellationToken = default)
    {
        var fn = _kernel.CreateFunctionFromPrompt(
            PromptTemplate,
            new OpenAIPromptExecutionSettings { Temperature = 0, MaxTokens = 600 }
        );

        var result = await _kernel.InvokeAsync(fn, new KernelArguments
        {
            ["context"] = context,
            ["question"] = question
        }, cancellationToken);

        return result.ToString();
    }
}

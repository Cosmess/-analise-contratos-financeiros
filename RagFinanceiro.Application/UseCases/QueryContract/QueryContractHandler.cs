using Microsoft.Extensions.Logging;
using RagFinanceiro.Domain.Repositories;
using RagFinanceiro.Domain.Services;
using RagFinanceiro.Domain.ValueObjects;

namespace RagFinanceiro.Application.UseCases.QueryContract;

public class QueryContractHandler(
    IContractRepository repository,
    ILlmQueryService llmQueryService,
    ILogger<QueryContractHandler> logger)
{
    public async Task<QueryContractResult> HandleAsync(
        QueryContractQuery query,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Consulta iniciada. TenantId: {TenantId} | QuestionLength: {QuestionLength}",
            query.TenantId, query.Question.Length);

        var tenantId = new TenantId(query.TenantId);
        var cpf = new Cpf(query.ClientCpf);

        var chunks = await repository.SearchAsync(tenantId, query.Question, cpf, cancellationToken);

        if (chunks.Count == 0)
        {
            logger.LogWarning(
                "Nenhum chunk encontrado para o CPF informado. TenantId: {TenantId}",
                query.TenantId);

            return new QueryContractResult(
                query.Question,
                "Nenhum contrato encontrado para este cliente.",
                Array.Empty<string>()
            );
        }

        logger.LogInformation(
            "Chunks recuperados: {ChunkCount}. Enviando para o LLM. TenantId: {TenantId}",
            chunks.Count, query.TenantId);

        var context = string.Join("\n\n", chunks.Select(c => c.Text));
        var answer = await llmQueryService.AskAsync(context, query.Question, cancellationToken);
        var sources = chunks.Select(c => c.Description).ToList();

        logger.LogInformation(
            "Resposta gerada com sucesso. TenantId: {TenantId} | Sources: {SourceCount}",
            query.TenantId, sources.Count);

        return new QueryContractResult(query.Question, answer, sources);
    }
}

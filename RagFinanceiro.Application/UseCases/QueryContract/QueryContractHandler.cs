using RagFinanceiro.Domain.Repositories;
using RagFinanceiro.Domain.Services;
using RagFinanceiro.Domain.ValueObjects;

namespace RagFinanceiro.Application.UseCases.QueryContract;

public class QueryContractHandler
{
    private readonly IContractRepository _repository;
    private readonly ILlmQueryService _llmQueryService;

    public QueryContractHandler(IContractRepository repository, ILlmQueryService llmQueryService)
    {
        _repository = repository;
        _llmQueryService = llmQueryService;
    }

    public async Task<QueryContractResult> HandleAsync(
        QueryContractQuery query,
        CancellationToken cancellationToken = default)
    {
        var tenantId = new TenantId(query.TenantId);
        var cpf = new Cpf(query.ClientCpf);

        var chunks = await _repository.SearchAsync(tenantId, query.Question, cpf, cancellationToken);

        if (chunks.Count == 0)
            return new QueryContractResult(
                query.Question,
                "Nenhum contrato encontrado para este cliente.",
                Array.Empty<string>()
            );

        var context = string.Join("\n\n", chunks.Select(c => c.Text));
        var answer = await _llmQueryService.AskAsync(context, query.Question, cancellationToken);
        var sources = chunks.Select(c => c.Description).ToList();

        return new QueryContractResult(query.Question, answer, sources);
    }
}

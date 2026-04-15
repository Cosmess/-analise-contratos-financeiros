namespace RagFinanceiro.Domain.Services;

public interface ILlmQueryService
{
    Task<string> AskAsync(string context, string question, CancellationToken cancellationToken = default);
}

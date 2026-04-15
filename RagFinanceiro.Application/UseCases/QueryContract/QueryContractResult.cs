namespace RagFinanceiro.Application.UseCases.QueryContract;

public record QueryContractResult(string Question, string Answer, IReadOnlyList<string> Sources);

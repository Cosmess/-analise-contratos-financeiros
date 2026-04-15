namespace RagFinanceiro.Application.UseCases.IngestContract;

public record IngestContractResult(int ChunksIndexed, string ContractId, string IndexedBy);

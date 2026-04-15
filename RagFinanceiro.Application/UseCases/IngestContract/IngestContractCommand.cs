namespace RagFinanceiro.Application.UseCases.IngestContract;

public record IngestContractCommand(
    Stream PdfStream,
    string ContractId,
    string ContractNumber,
    string ClientName,
    string ClientCpf,
    string TenantId
);

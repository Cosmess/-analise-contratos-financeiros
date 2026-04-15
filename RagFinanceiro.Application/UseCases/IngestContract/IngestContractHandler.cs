using Microsoft.Extensions.Logging;
using RagFinanceiro.Domain.Entities;
using RagFinanceiro.Domain.Repositories;

namespace RagFinanceiro.Application.UseCases.IngestContract;

public class IngestContractHandler(
    IContractRepository repository,
    ILogger<IngestContractHandler> logger)
{
    public async Task<IngestContractResult> HandleAsync(
        IngestContractCommand command,
        string indexedBy,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Iniciando ingestao. ContractId: {ContractId} | TenantId: {TenantId}",
            command.ContractId, command.TenantId);

        var contract = Contract.Create(
            command.ContractId,
            command.TenantId,
            command.ClientCpf,
            command.ContractNumber,
            command.ClientName
        );

        await repository.IngestAsync(command.PdfStream, contract, cancellationToken);

        logger.LogInformation(
            "Ingestao concluida. ContractId: {ContractId} | Chunks: {ChunkCount} | IndexadoPor: {IndexedBy}",
            contract.ContractId.Value, contract.ChunkCount, indexedBy);

        return new IngestContractResult(
            ChunksIndexed: contract.ChunkCount,
            ContractId: command.ContractId,
            IndexedBy: indexedBy
        );
    }
}

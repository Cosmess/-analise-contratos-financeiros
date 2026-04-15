using RagFinanceiro.Domain.Entities;
using RagFinanceiro.Domain.Repositories;
using RagFinanceiro.Domain.Services;

namespace RagFinanceiro.Application.UseCases.IngestContract;

public class IngestContractHandler
{
    private readonly IContractRepository _repository;
    private readonly IPdfReaderService _pdfReader;

    public IngestContractHandler(IContractRepository repository, IPdfReaderService pdfReader)
    {
        _repository = repository;
        _pdfReader = pdfReader;
    }

    public async Task<IngestContractResult> HandleAsync(
        IngestContractCommand command,
        string indexedBy,
        CancellationToken cancellationToken = default)
    {
        var contract = Contract.Create(
            command.ContractId,
            command.TenantId,
            command.ClientCpf,
            command.ContractNumber,
            command.ClientName
        );

        await _repository.IngestAsync(command.PdfStream, contract, cancellationToken);

        return new IngestContractResult(
            ChunksIndexed: contract.ChunkCount,
            ContractId: command.ContractId,
            IndexedBy: indexedBy
        );
    }
}

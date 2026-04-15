using RagFinanceiro.Domain.Entities;
using RagFinanceiro.Domain.ValueObjects;

namespace RagFinanceiro.Domain.Repositories;

public interface IContractRepository
{
    Task IngestAsync(Stream pdfStream, Contract contract, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractChunk>> SearchAsync(
        TenantId tenantId,
        string query,
        Cpf cpf,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(ContractId contractId, TenantId tenantId, CancellationToken cancellationToken = default);
}

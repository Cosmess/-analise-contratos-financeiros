#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020

using Microsoft.SemanticKernel.Memory;
using RagFinanceiro.Domain.Entities;
using RagFinanceiro.Domain.Repositories;
using RagFinanceiro.Domain.Services;
using RagFinanceiro.Domain.ValueObjects;

namespace RagFinanceiro.Infrastructure.Persistence;

public class SemanticKernelContractRepository : IContractRepository
{
    private readonly ContractMemoryStore _memoryStore;
    private readonly IPdfReaderService _pdfReader;

    public SemanticKernelContractRepository(ContractMemoryStore memoryStore, IPdfReaderService pdfReader)
    {
        _memoryStore = memoryStore;
        _pdfReader = pdfReader;
    }

    public async Task IngestAsync(Stream pdfStream, Contract contract, CancellationToken cancellationToken = default)
    {
        var tmpPath = Path.GetTempFileName() + ".pdf";
        await using (var fs = File.Create(tmpPath))
            await pdfStream.CopyToAsync(fs, cancellationToken);

        try
        {
            var chunks = _pdfReader.ReadAsChunks(tmpPath);
            contract.UpdateChunkCount(chunks.Count);

            for (int i = 0; i < chunks.Count; i++)
            {
                await _memoryStore.SaveChunkAsync(
                    tenantId: contract.TenantId.Value,
                    chunkId: $"{contract.ContractId.Value}_c{i}",
                    text: chunks[i],
                    description: $"Contrato:{contract.Number}|Cliente:{contract.ClientName}|CPF:{contract.Cpf.Value}"
                );
            }
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }

    public async Task<IReadOnlyList<ContractChunk>> SearchAsync(
        TenantId tenantId,
        string query,
        Cpf cpf,
        CancellationToken cancellationToken = default)
    {
        var results = await _memoryStore.SearchAsync(tenantId.Value, query).ToListAsync(cancellationToken);

        return results
            .Where(r => r.Metadata.Description.Contains(cpf.Value))
            .Select(r => new ContractChunk(r.Metadata.Text, r.Metadata.Description, r.Relevance))
            .ToList();
    }

    public async Task DeleteAsync(ContractId contractId, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        await _memoryStore.RemoveAsync(tenantId.Value, contractId.Value);
    }
}

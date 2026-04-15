#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Memory;
using RagFinanceiro.Domain.Entities;
using RagFinanceiro.Domain.Repositories;
using RagFinanceiro.Domain.Services;
using RagFinanceiro.Domain.ValueObjects;

namespace RagFinanceiro.Infrastructure.Persistence;

public class SemanticKernelContractRepository(
    ContractMemoryStore memoryStore,
    IPdfReaderService pdfReader,
    ILogger<SemanticKernelContractRepository> logger) : IContractRepository
{
    public async Task IngestAsync(Stream pdfStream, Contract contract, CancellationToken cancellationToken = default)
    {
        var tmpPath = Path.GetTempFileName() + ".pdf";

        logger.LogDebug(
            "Salvando PDF temporario em {TmpPath} para contrato {ContractId}",
            tmpPath, contract.ContractId.Value);

        await using (var fs = File.Create(tmpPath))
            await pdfStream.CopyToAsync(fs, cancellationToken);

        try
        {
            var chunks = pdfReader.ReadAsChunks(tmpPath);
            contract.UpdateChunkCount(chunks.Count);

            logger.LogInformation(
                "Indexando {ChunkCount} chunks do contrato {ContractId} no tenant {TenantId}",
                chunks.Count, contract.ContractId.Value, contract.TenantId.Value);

            for (int i = 0; i < chunks.Count; i++)
            {
                await memoryStore.SaveChunkAsync(
                    tenantId: contract.TenantId.Value,
                    chunkId: $"{contract.ContractId.Value}_c{i}",
                    text: chunks[i],
                    description: $"Contrato:{contract.Number}|Cliente:{contract.ClientName}|CPF:{contract.Cpf.Value}"
                );
            }

            logger.LogInformation(
                "Todos os chunks indexados com sucesso. ContractId: {ContractId} | TenantId: {TenantId}",
                contract.ContractId.Value, contract.TenantId.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Ingestao cancelada. ContractId: {ContractId} | TenantId: {TenantId}",
                contract.ContractId.Value, contract.TenantId.Value);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Falha ao indexar contrato {ContractId} no tenant {TenantId}",
                contract.ContractId.Value, contract.TenantId.Value);
            throw;
        }
        finally
        {
            TryDeleteTemporaryFile(tmpPath);
        }
    }

    public async Task<IReadOnlyList<ContractChunk>> SearchAsync(
        TenantId tenantId,
        string query,
        Cpf cpf,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Buscando chunks similares. TenantId: {TenantId} | QueryLength: {QueryLength}",
            tenantId.Value, query.Length);

        try
        {
            var results = await memoryStore.SearchAsync(tenantId.Value, query).ToListAsync(cancellationToken);

            var filtered = results
                .Where(r => r.Metadata.Description.Contains(cpf.Value))
                .Select(r => new ContractChunk(r.Metadata.Text, r.Metadata.Description, r.Relevance))
                .ToList();

            logger.LogDebug(
                "Busca concluida. Total: {Total} | FiltradosPorCpf: {Filtered} | TenantId: {TenantId}",
                results.Count, filtered.Count, tenantId.Value);

            return filtered;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Busca vetorial cancelada. TenantId: {TenantId}",
                tenantId.Value);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Falha na busca vetorial. TenantId: {TenantId} | QueryLength: {QueryLength}",
                tenantId.Value, query.Length);
            throw;
        }
    }

    public async Task DeleteAsync(ContractId contractId, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Removendo contrato do indice. ContractId: {ContractId} | TenantId: {TenantId}",
            contractId.Value, tenantId.Value);

        try
        {
            await memoryStore.RemoveAsync(tenantId.Value, contractId.Value);

            logger.LogInformation(
                "Contrato removido do indice vetorial. ContractId: {ContractId} | TenantId: {TenantId}",
                contractId.Value, tenantId.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Remocao de contrato cancelada. ContractId: {ContractId} | TenantId: {TenantId}",
                contractId.Value, tenantId.Value);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Falha ao remover contrato {ContractId} do tenant {TenantId}",
                contractId.Value, tenantId.Value);
            throw;
        }
    }

    private void TryDeleteTemporaryFile(string tmpPath)
    {
        try
        {
            if (!File.Exists(tmpPath))
                return;

            File.Delete(tmpPath);
            logger.LogDebug("Arquivo temporario removido: {TmpPath}", tmpPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao remover arquivo temporario: {TmpPath}", tmpPath);
        }
    }
}

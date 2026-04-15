#pragma warning disable SKEXP0001, SKEXP0020

using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace RagFinanceiro.Infrastructure.Persistence;

public class ContractMemoryStore : IDisposable
{
    private readonly SemanticTextMemory _memory;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<ContractMemoryStore> _logger;

    public ContractMemoryStore(
        string connectionString,
        ITextEmbeddingGenerationService embeddingService,
        ILogger<ContractMemoryStore> logger)
    {
        _logger = logger;

        _logger.LogInformation("Inicializando ContractMemoryStore com PostgreSQL/pgvector.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        _dataSource = dataSourceBuilder.Build();
        var store = new PostgresMemoryStore(_dataSource, vectorSize: 1536, schema: "public");
        _memory = new SemanticTextMemory(store, embeddingService);

        _logger.LogInformation("ContractMemoryStore inicializado com sucesso.");
    }

    public async Task SaveChunkAsync(string tenantId, string chunkId, string text, string description)
    {
        var collection = GetTenantCollection(tenantId);

        _logger.LogDebug(
            "Salvando chunk na memoria vetorial. Collection: {Collection} | ChunkId: {ChunkId} | TextLength: {TextLength}",
            collection, chunkId, text.Length);

        try
        {
            await _memory.SaveInformationAsync(
                collection: collection,
                id: chunkId,
                text: text,
                description: description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao salvar chunk na memoria vetorial. Collection: {Collection} | ChunkId: {ChunkId}",
                collection, chunkId);
            throw;
        }
    }

    public IAsyncEnumerable<MemoryQueryResult> SearchAsync(
        string tenantId, string query, int limit = 4, double minScore = 0.72)
    {
        var collection = GetTenantCollection(tenantId);

        _logger.LogDebug(
            "Consultando memoria vetorial. Collection: {Collection} | Limit: {Limit} | MinScore: {MinScore}",
            collection, limit, minScore);

        return _memory.SearchAsync(
            collection: collection,
            query: query,
            limit: limit,
            minRelevanceScore: minScore);
    }

    public async Task RemoveAsync(string tenantId, string chunkId)
    {
        var collection = GetTenantCollection(tenantId);

        _logger.LogDebug(
            "Removendo chunk da memoria vetorial. Collection: {Collection} | ChunkId: {ChunkId}",
            collection, chunkId);

        try
        {
            await _memory.RemoveAsync(
                collection: collection,
                key: chunkId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao remover chunk da memoria vetorial. Collection: {Collection} | ChunkId: {ChunkId}",
                collection, chunkId);
            throw;
        }
    }

    public void Dispose()
    {
        _logger.LogDebug("Liberando recursos do ContractMemoryStore.");
        _dataSource.Dispose();
    }

    private static string GetTenantCollection(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId e obrigatorio.", nameof(tenantId));

        return $"tenant_{tenantId}";
    }
}

#pragma warning disable SKEXP0001, SKEXP0020

using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Npgsql;

namespace RagFinanceiro.Infrastructure.Persistence;

public class ContractMemoryStore : IDisposable
{
    private readonly SemanticTextMemory _memory;
    private readonly NpgsqlDataSource _dataSource;

    public ContractMemoryStore(string connectionString, ITextEmbeddingGenerationService embeddingService)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        _dataSource = dataSourceBuilder.Build();
        var store = new PostgresMemoryStore(_dataSource, vectorSize: 1536, schema: "public");
        _memory = new SemanticTextMemory(store, embeddingService);
    }

    public Task SaveChunkAsync(string tenantId, string chunkId, string text, string description)
        => _memory.SaveInformationAsync(
            collection: $"tenant_{tenantId}",
            id: chunkId,
            text: text,
            description: description);

    public IAsyncEnumerable<MemoryQueryResult> SearchAsync(
        string tenantId, string query, int limit = 4, double minScore = 0.72)
        => _memory.SearchAsync(
            collection: $"tenant_{tenantId}",
            query: query,
            limit: limit,
            minRelevanceScore: minScore);

    public Task RemoveAsync(string tenantId, string chunkId)
        => _memory.RemoveAsync(
            collection: $"tenant_{tenantId}",
            key: chunkId);

    public void Dispose() => _dataSource.Dispose();
}

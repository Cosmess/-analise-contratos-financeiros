namespace RagFinanceiro.Domain.Services;

public interface IChunkingService
{
    IReadOnlyList<string> Split(string text, int chunkSize, int overlap);
}

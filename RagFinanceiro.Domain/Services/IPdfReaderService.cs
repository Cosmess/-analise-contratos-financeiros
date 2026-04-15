namespace RagFinanceiro.Domain.Services;

public interface IPdfReaderService
{
    IReadOnlyList<string> ReadAsChunks(string filePath, int chunkSize = 600, int overlap = 120);
}

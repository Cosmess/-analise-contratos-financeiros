using Microsoft.Extensions.Logging;
using RagFinanceiro.Domain.Services;
using UglyToad.PdfPig;

namespace RagFinanceiro.Infrastructure.Pdf;

public class PdfPigReaderService(IChunkingService chunkingService, ILogger<PdfPigReaderService> logger) : IPdfReaderService
{
    public IReadOnlyList<string> ReadAsChunks(string filePath, int chunkSize = 600, int overlap = 120)
    {
        logger.LogDebug("Iniciando leitura do PDF: {FilePath}", filePath);

        try
        {
            var fullText = ReadFullText(filePath);

            if (string.IsNullOrWhiteSpace(fullText))
            {
                logger.LogWarning("PDF sem conteúdo legível extraído: {FilePath}", filePath);
                return Array.Empty<string>();
            }

            var chunks = chunkingService.Split(fullText, chunkSize, overlap);

            logger.LogInformation(
                "PDF processado. FilePath: {FilePath} | TextLength: {TextLength} | Chunks: {ChunkCount}",
                filePath, fullText.Length, chunks.Count);

            return chunks;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar PDF: {FilePath}", filePath);
            throw;
        }
    }

    private string ReadFullText(string filePath)
    {
        using var pdf = PdfDocument.Open(filePath);
        var pages = new List<string>();

        foreach (var page in pdf.GetPages())
        {
            var text = string.Join(" ", page.GetWords().Select(w => w.Text)).Trim();
            if (text.Length > 50)
                pages.Add(text);
        }

        logger.LogDebug(
            "PDF lido. FilePath: {FilePath} | PaginasComConteudo: {PageCount}",
            filePath, pages.Count);

        return string.Join("\n\n", pages);
    }
}

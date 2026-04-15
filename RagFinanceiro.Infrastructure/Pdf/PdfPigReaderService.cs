using RagFinanceiro.Domain.Services;
using UglyToad.PdfPig;

namespace RagFinanceiro.Infrastructure.Pdf;

public class PdfPigReaderService : IPdfReaderService
{
    private readonly IChunkingService _chunkingService;

    public PdfPigReaderService(IChunkingService chunkingService)
    {
        _chunkingService = chunkingService;
    }

    public IReadOnlyList<string> ReadAsChunks(string filePath, int chunkSize = 600, int overlap = 120)
    {
        var fullText = ReadFullText(filePath);
        return _chunkingService.Split(fullText, chunkSize, overlap);
    }

    private static string ReadFullText(string filePath)
    {
        using var pdf = PdfDocument.Open(filePath);
        var pages = new List<string>();

        foreach (var page in pdf.GetPages())
        {
            var text = string.Join(" ", page.GetWords().Select(w => w.Text)).Trim();
            if (text.Length > 50)
                pages.Add(text);
        }

        return string.Join("\n\n", pages);
    }
}

using RagFinanceiro.Domain.Services;
using System.Text;

namespace RagFinanceiro.Infrastructure.Pdf;

public class TextChunkingService : IChunkingService
{
    public IReadOnlyList<string> Split(string text, int chunkSize, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "ChunkSize deve ser maior que zero.");

        if (overlap < 0)
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap nao pode ser negativo.");

        if (overlap >= chunkSize)
            throw new ArgumentException("Overlap deve ser menor que chunkSize.", nameof(overlap));

        var chunks = new List<string>();
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var buffer = new StringBuilder();

        foreach (var p in paragraphs)
        {
            if (buffer.Length + p.Length > chunkSize)
            {
                if (buffer.Length > 0)
                    chunks.Add(buffer.ToString().Trim());

                var last = buffer.ToString();
                buffer.Clear();

                if (last.Length > overlap)
                    buffer.Append(last[^overlap..]).Append(' ');
            }
            buffer.Append(p).Append(' ');
        }

        if (buffer.Length > 0)
            chunks.Add(buffer.ToString().Trim());

        return chunks;
    }
}

using RagFinanceiro.Domain.Services;
using System.Text;

namespace RagFinanceiro.Infrastructure.Pdf;

public class TextChunkingService : IChunkingService
{
    public IReadOnlyList<string> Split(string text, int chunkSize, int overlap)
    {
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

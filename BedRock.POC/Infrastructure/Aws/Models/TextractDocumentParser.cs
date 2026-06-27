using Amazon.Textract;
using Amazon.Textract.Model;

namespace BedRock.POC.Infrastructure.Aws.Models;

public static class TextractDocumentParser
{
    public static IReadOnlyList<string> ExtractPages(IReadOnlyList<Block> blocks)
    {
        var pages = new Dictionary<int, List<string>>();

        foreach (var block in blocks)
        {
            if (block.BlockType != BlockType.LINE) continue;

            var pageNum = block.Page ?? 1;
            if (!pages.ContainsKey(pageNum))
                pages[pageNum] = [];
            pages[pageNum].Add(block.Text ?? string.Empty);
        }

        return pages.OrderBy(p => p.Key)
                    .Select(p => string.Join("\n", p.Value))
                    .ToList();
    }
}

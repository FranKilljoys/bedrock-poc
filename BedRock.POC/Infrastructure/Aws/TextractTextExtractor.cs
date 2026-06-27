using BedRock.POC.Application.Interfaces;
using BedRock.POC.Domain.Interfaces;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Infrastructure.Aws.Models;

namespace BedRock.POC.Infrastructure.Aws;

public class TextractTextExtractor(
    ILogger<TextractTextExtractor> logger,
    ITextractAdapter adapter,
    ITextractJobPoller poller,
    ITextractResultCache cache) : ITextExtractor
{
    public async Task<IReadOnlyList<string>> ExtractPagesAsync(StorageLocation source, CancellationToken ct = default)
    {
        var cached = await cache.GetCachedPagesAsync(source, ct);
        if (cached.Count > 0) return cached;

        var jobId = await adapter.StartDetectionJobAsync(Guid.NewGuid().ToString(), source);
        await poller.WaitForCompletionAsync(jobId, source, ct);

        var doc = await adapter.GetDetectionResultAsync(jobId, source);
        if (doc is null)
        {
            logger.LogWarning("Textract returned no document for job {JobId}", jobId);
            return [];
        }

        var pages = TextractDocumentParser.ExtractPages(doc.Blocks);
        await cache.StorePagesAsync(source, pages, ct);

        return pages;
    }
}

using Amazon.S3;
using Amazon.S3.Model;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Domain.ValueObjects;

namespace BedRock.POC.Infrastructure.Aws;

public class TextractResultCache(ILogger<TextractResultCache> logger, IAmazonS3 s3Client) : ITextractResultCache
{
    public async Task<IReadOnlyList<string>> GetCachedPagesAsync(StorageLocation source, CancellationToken ct = default)
    {
        var prefix = CachePrefix(source);
        var keys = await ListCacheKeysAsync(source.Bucket, prefix, ct);
        if (keys.Count == 0) return [];

        logger.LogInformation("Cache hit for {FileName} — {Count} pages", source.FileName, keys.Count);
        var pages = new List<string>(keys.Count);

        foreach (var key in keys.OrderBy(k => k))
        {
            var response = await s3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = source.Bucket,
                Key = key
            }, ct);
            using var reader = new StreamReader(response.ResponseStream);
            pages.Add(await reader.ReadToEndAsync(ct));
        }

        return pages;
    }

    public async Task StorePagesAsync(StorageLocation source, IReadOnlyList<string> pages, CancellationToken ct = default)
    {
        var baseName = Path.GetFileNameWithoutExtension(source.FileName);
        for (var i = 0; i < pages.Count; i++)
        {
            var key = $"{baseName}/{baseName}_{i}.txt";
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = source.Bucket,
                Key = key,
                ContentBody = pages[i]
            }, ct);
        }
        logger.LogInformation("Cached {Count} pages for {FileName}", pages.Count, source.FileName);
    }

    private async Task<List<string>> ListCacheKeysAsync(string bucket, string prefix, CancellationToken ct)
    {
        var keys = new List<string>();
        string continuationToken = null;

        do
        {
            var response = await s3Client.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken
            }, ct);

            keys.AddRange(response.S3Objects.Select(o => o.Key));
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken != null);

        return keys;
    }

    private static string CachePrefix(StorageLocation source) =>
        $"{Path.GetFileNameWithoutExtension(source.FileName)}/";
}

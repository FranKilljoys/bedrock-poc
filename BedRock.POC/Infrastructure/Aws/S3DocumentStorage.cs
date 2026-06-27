using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using BedRock.POC.Domain.Interfaces;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BedRock.POC.Infrastructure.Aws;

public class S3DocumentStorage(
    ILogger<S3DocumentStorage> logger,
    IAmazonS3 s3Client,
    ITransferUtility transferUtility,
    IOptions<AwsOptions> options) : IDocumentStorage
{
    private readonly string _bucket = options.Value.S3BucketName;

    public async Task<StorageLocation> UploadAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var key = fileName;
        logger.LogInformation("Uploading {FileName} to s3://{Bucket}/{Key}", fileName, _bucket, key);

        var request = new TransferUtilityUploadRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream
        };

        await transferUtility.UploadAsync(request, ct);
        logger.LogInformation("Upload complete: s3://{Bucket}/{Key}", _bucket, key);

        return new StorageLocation(_bucket, key);
    }

    public Task<Stream> DownloadAsync(StorageLocation location, CancellationToken ct = default)
    {
        logger.LogInformation("Downloading s3://{Bucket}/{Key}", location.Bucket, location.Key);
        return Task.FromResult<Stream>(transferUtility.OpenStream(location.Bucket, location.Key));
    }

    public async Task<T> DownloadJsonAsync<T>(StorageLocation location, CancellationToken ct = default)
    {
        await using var stream = await DownloadAsync(location, ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
    }

    public async Task<List<StorageLocation>> ListAsync(string bucket, string prefix, CancellationToken ct = default)
    {
        var result = new List<StorageLocation>();
        string continuationToken = null;

        do
        {
            var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken
            }, ct);

            result.AddRange(response.S3Objects.Select(o => new StorageLocation(o.BucketName, o.Key)));
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken != null);

        return result;
    }

    public async Task StoreTextAsync(StorageLocation location, string content, CancellationToken ct = default)
    {
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = location.Bucket,
            Key = location.Key,
            ContentBody = content
        }, ct);
    }

    public async Task<string> ReadTextAsync(StorageLocation location, CancellationToken ct = default)
    {
        var response = await s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = location.Bucket,
            Key = location.Key
        }, ct);

        using var reader = new StreamReader(response.ResponseStream);
        return await reader.ReadToEndAsync(ct);
    }
}

using Amazon.S3;
using Amazon.S3.Model;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Infrastructure.Aws;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BedRock.POC.Tests.Infrastructure;

public class TextractResultCacheTests
{
    private readonly Mock<IAmazonS3> _s3 = new();
    private readonly StorageLocation _source = new("bucket", "folder/document.pdf");

    private TextractResultCache CreateCache() =>
        new(NullLogger<TextractResultCache>.Instance, _s3.Object);

    private void SetupListObjects(params string[] keys)
    {
        _s3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = keys.Select(k => new S3Object { Key = k }).ToList(),
                IsTruncated = false
            });
    }

    private void SetupGetObject(string key, string content)
    {
        _s3.Setup(s => s.GetObjectAsync(It.Is<GetObjectRequest>(r => r.Key == key), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse
            {
                ResponseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content))
            });
    }

    [Fact]
    public async Task GetCachedPagesAsync_ReturnsEmpty_WhenNoCacheKeys()
    {
        SetupListObjects();

        var result = await CreateCache().GetCachedPagesAsync(_source);

        result.Should().BeEmpty();
        _s3.Verify(s => s.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCachedPagesAsync_ReturnsPagesInOrder_WhenCacheExists()
    {
        SetupListObjects("document/document_1.txt", "document/document_0.txt");
        SetupGetObject("document/document_0.txt", "page one content");
        SetupGetObject("document/document_1.txt", "page two content");

        var result = await CreateCache().GetCachedPagesAsync(_source);

        result.Should().HaveCount(2);
        result[0].Should().Be("page one content");
        result[1].Should().Be("page two content");
    }

    [Fact]
    public async Task StorePagesAsync_CallsPutObjectForEachPage()
    {
        _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        await CreateCache().StorePagesAsync(_source, ["page 0 text", "page 1 text"]);

        _s3.Verify(s => s.PutObjectAsync(
            It.Is<PutObjectRequest>(r => r.BucketName == "bucket" && r.ContentBody == "page 0 text"),
            It.IsAny<CancellationToken>()), Times.Once);
        _s3.Verify(s => s.PutObjectAsync(
            It.Is<PutObjectRequest>(r => r.BucketName == "bucket" && r.ContentBody == "page 1 text"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Infrastructure.Aws;
using BedRock.POC.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BedRock.POC.Tests.Infrastructure;

public class S3DocumentStorageTests
{
    private const string Bucket = "test-bucket";
    private readonly Mock<IAmazonS3> _s3 = new();
    private readonly Mock<ITransferUtility> _transfer = new();

    private S3DocumentStorage CreateStorage() => new(
        NullLogger<S3DocumentStorage>.Instance,
        _s3.Object,
        _transfer.Object,
        Options.Create(new AwsOptions { Region = "us-east-1", S3BucketName = Bucket }));

    [Fact]
    public async Task UploadAsync_CallsTransferUtilityAndReturnsLocation()
    {
        _transfer.Setup(t => t.UploadAsync(It.IsAny<TransferUtilityUploadRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateStorage().UploadAsync(Stream.Null, "document.pdf");

        result.Bucket.Should().Be(Bucket);
        result.Key.Should().Be("document.pdf");
        _transfer.Verify(t => t.UploadAsync(
            It.Is<TransferUtilityUploadRequest>(r => r.BucketName == Bucket && r.Key == "document.pdf"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedLocations()
    {
        _s3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [new() { BucketName = Bucket, Key = "a.pdf" }, new() { BucketName = Bucket, Key = "b.pdf" }],
                IsTruncated = false
            });

        var result = await CreateStorage().ListAsync(Bucket, "");

        result.Should().HaveCount(2);
        result.Should().Contain(l => l.Key == "a.pdf");
        result.Should().Contain(l => l.Key == "b.pdf");
    }

    [Fact]
    public async Task ListAsync_PaginatesUntilNotTruncated()
    {
        var callCount = 0;
        _s3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new ListObjectsV2Response { S3Objects = [new() { BucketName = Bucket, Key = "p1.pdf" }], IsTruncated = true, NextContinuationToken = "token1" }
                    : new ListObjectsV2Response { S3Objects = [new() { BucketName = Bucket, Key = "p2.pdf" }], IsTruncated = false };
            });

        var result = await CreateStorage().ListAsync(Bucket, "");

        result.Should().HaveCount(2);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task StoreTextAsync_CallsPutObject()
    {
        var location = new StorageLocation(Bucket, "out/file.txt");
        _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        await CreateStorage().StoreTextAsync(location, "hello");

        _s3.Verify(s => s.PutObjectAsync(
            It.Is<PutObjectRequest>(r => r.BucketName == Bucket && r.Key == "out/file.txt" && r.ContentBody == "hello"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReadTextAsync_ReturnsTextFromStream()
    {
        var location = new StorageLocation(Bucket, "file.txt");
        var content = "extracted text";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        _s3.Setup(s => s.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse { ResponseStream = stream });

        var result = await CreateStorage().ReadTextAsync(location);

        result.Should().Be(content);
    }
}

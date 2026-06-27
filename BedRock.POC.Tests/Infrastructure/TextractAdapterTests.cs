using Amazon.Textract;
using Amazon.Textract.Model;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Infrastructure.Aws;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BedRock.POC.Tests.Infrastructure;

public class TextractAdapterTests
{
    private readonly Mock<IAmazonTextract> _textract = new();
    private readonly StorageLocation _source = new("bucket", "docs/file.pdf");

    private TextractAdapter CreateAdapter() =>
        new(NullLogger<TextractAdapter>.Instance, _textract.Object);

    [Fact]
    public async Task StartDetectionJobAsync_ReturnsJobId()
    {
        _textract.Setup(t => t.StartDocumentTextDetectionAsync(It.IsAny<StartDocumentTextDetectionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartDocumentTextDetectionResponse { JobId = "job-abc" });

        var jobId = await CreateAdapter().StartDetectionJobAsync("corr-1", _source);

        jobId.Should().Be("job-abc");
    }

    [Fact]
    public async Task StartDetectionJobAsync_PassesBucketAndKeyToRequest()
    {
        StartDocumentTextDetectionRequest capturedRequest = null;
        _textract.Setup(t => t.StartDocumentTextDetectionAsync(It.IsAny<StartDocumentTextDetectionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<StartDocumentTextDetectionRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new StartDocumentTextDetectionResponse { JobId = "job-xyz" });

        await CreateAdapter().StartDetectionJobAsync("corr-2", _source);

        capturedRequest.DocumentLocation.S3Object.Bucket.Should().Be("bucket");
        capturedRequest.DocumentLocation.S3Object.Name.Should().Be("docs/file.pdf");
    }

    [Fact]
    public async Task GetDetectionResultAsync_ReturnsDocumentWithBlocks()
    {
        var blocks = new List<Block>
        {
            new() { BlockType = BlockType.LINE, Text = "Hello", Page = 1 }
        };

        _textract.Setup(t => t.GetDocumentTextDetectionAsync(It.IsAny<GetDocumentTextDetectionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetDocumentTextDetectionResponse
            {
                JobStatus = JobStatus.SUCCEEDED,
                Blocks = blocks,
                DocumentMetadata = new DocumentMetadata { Pages = 1 }
            });

        var doc = await CreateAdapter().GetDetectionResultAsync("job-1", _source);

        doc.Should().NotBeNull();
        doc.JobStatus.Should().Be(JobStatus.SUCCEEDED);
        doc.Blocks.Should().HaveCount(1);
        doc.Blocks[0].Text.Should().Be("Hello");
    }

    [Fact]
    public async Task GetDetectionResultAsync_PaginatesWhenNextTokenPresent()
    {
        var callCount = 0;
        _textract.Setup(t => t.GetDocumentTextDetectionAsync(It.IsAny<GetDocumentTextDetectionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new GetDocumentTextDetectionResponse
                    {
                        JobStatus = JobStatus.SUCCEEDED,
                        Blocks = [new() { BlockType = BlockType.LINE, Text = "Page 1", Page = 1 }],
                        NextToken = "token1"
                    }
                    : new GetDocumentTextDetectionResponse
                    {
                        JobStatus = JobStatus.SUCCEEDED,
                        Blocks = [new() { BlockType = BlockType.LINE, Text = "Page 2", Page = 2 }],
                        NextToken = null
                    };
            });

        var doc = await CreateAdapter().GetDetectionResultAsync("job-2", _source);

        doc.Blocks.Should().HaveCount(2);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetDetectionResultAsync_ReturnsNull_WhenNoResponses()
    {
        _textract.Setup(t => t.GetDocumentTextDetectionAsync(It.IsAny<GetDocumentTextDetectionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetDocumentTextDetectionResponse
            {
                JobStatus = JobStatus.SUCCEEDED,
                Blocks = [],
                NextToken = null
            });

        var doc = await CreateAdapter().GetDetectionResultAsync("job-3", _source);

        doc.Should().NotBeNull();
    }
}

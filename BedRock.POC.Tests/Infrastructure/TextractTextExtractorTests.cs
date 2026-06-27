using Amazon.Textract;
using Amazon.Textract.Model;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Infrastructure.Aws;
using BedRock.POC.Infrastructure.Aws.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BedRock.POC.Tests.Infrastructure;

public class TextractTextExtractorTests
{
    private readonly Mock<ITextractAdapter> _adapter = new();
    private readonly Mock<ITextractJobPoller> _poller = new();
    private readonly Mock<ITextractResultCache> _cache = new();
    private readonly StorageLocation _source = new("bucket", "docs/file.pdf");

    private TextractTextExtractor CreateExtractor() =>
        new(NullLogger<TextractTextExtractor>.Instance, _adapter.Object, _poller.Object, _cache.Object);

    private static TextractDocument DocWithLines(params string[] lines)
    {
        var blocks = lines.Select((t, i) => new Block
        {
            BlockType = BlockType.LINE,
            Text = t,
            Page = 1
        }).ToList();

        return TextractDocument.FromDetectionResponse(
            new GetDocumentTextDetectionResponse { JobStatus = JobStatus.SUCCEEDED, Blocks = blocks },
            new StorageLocation("bucket", "docs/file.pdf"), 1);
    }

    [Fact]
    public async Task ExtractPagesAsync_ReturnsCachedPages_WithoutStartingJob()
    {
        _cache.Setup(c => c.GetCachedPagesAsync(_source, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["cached page 1", "cached page 2"]);

        var result = await CreateExtractor().ExtractPagesAsync(_source);

        result.Should().BeEquivalentTo(["cached page 1", "cached page 2"]);
        _adapter.Verify(a => a.StartDetectionJobAsync(It.IsAny<string>(), It.IsAny<StorageLocation>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExtractPagesAsync_StartsJobAndReturnsPages_WhenCacheIsEmpty()
    {
        _cache.Setup(c => c.GetCachedPagesAsync(_source, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _adapter.Setup(a => a.StartDetectionJobAsync(It.IsAny<string>(), _source, null, null)).ReturnsAsync("job-123");
        _poller.Setup(p => p.WaitForCompletionAsync("job-123", _source, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _adapter.Setup(a => a.GetDetectionResultAsync("job-123", _source)).ReturnsAsync(DocWithLines("Line A", "Line B"));
        _cache.Setup(c => c.StorePagesAsync(_source, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateExtractor().ExtractPagesAsync(_source);

        result.Should().HaveCount(1);
        result[0].Should().Contain("Line A").And.Contain("Line B");
    }

    [Fact]
    public async Task ExtractPagesAsync_ReturnsEmpty_WhenAdapterReturnsNullDocument()
    {
        _cache.Setup(c => c.GetCachedPagesAsync(_source, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _adapter.Setup(a => a.StartDetectionJobAsync(It.IsAny<string>(), _source, null, null)).ReturnsAsync("job-456");
        _poller.Setup(p => p.WaitForCompletionAsync("job-456", _source, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _adapter.Setup(a => a.GetDetectionResultAsync("job-456", _source)).ReturnsAsync((TextractDocument)null);

        var result = await CreateExtractor().ExtractPagesAsync(_source);

        result.Should().BeEmpty();
        _cache.Verify(c => c.StorePagesAsync(It.IsAny<StorageLocation>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

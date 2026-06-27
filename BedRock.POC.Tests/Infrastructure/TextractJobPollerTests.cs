using BedRock.POC.Application.Interfaces;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Infrastructure.Aws;
using BedRock.POC.Infrastructure.Aws.Models;
using BedRock.POC.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BedRock.POC.Tests.Infrastructure;

public class TextractJobPollerTests
{
    private readonly Mock<ITextractAdapter> _adapter = new();
    private readonly StorageLocation _source = new("bucket", "doc.pdf");

    private TextractJobPoller CreatePoller(int maxAttempts = 5, int intervalSeconds = 0) =>
        new(NullLogger<TextractJobPoller>.Instance, _adapter.Object,
            Options.Create(new TextractOptions { MaxPollingAttempts = maxAttempts, PollingIntervalSeconds = intervalSeconds }));

    private static TextractDocument SucceededDoc() =>
        TextractDocument.FromDetectionResponse(
            new Amazon.Textract.Model.GetDocumentTextDetectionResponse
            {
                JobStatus = Amazon.Textract.JobStatus.SUCCEEDED,
                Blocks = []
            },
            new StorageLocation("bucket", "doc.pdf"), 0);

    private static TextractDocument InProgressDoc() =>
        TextractDocument.FromDetectionResponse(
            new Amazon.Textract.Model.GetDocumentTextDetectionResponse
            {
                JobStatus = Amazon.Textract.JobStatus.IN_PROGRESS,
                Blocks = []
            },
            new StorageLocation("bucket", "doc.pdf"), 0);

    [Fact]
    public async Task WaitForCompletionAsync_CompletesOnFirstPoll_WhenStatusIsSucceeded()
    {
        _adapter.Setup(a => a.GetDetectionResultAsync("job1", _source))
            .ReturnsAsync(SucceededDoc());

        await CreatePoller().Invoking(p => p.WaitForCompletionAsync("job1", _source))
            .Should().NotThrowAsync();

        _adapter.Verify(a => a.GetDetectionResultAsync("job1", _source), Times.Once);
    }

    [Fact]
    public async Task WaitForCompletionAsync_PollsMultipleTimes_UntilSucceeded()
    {
        var callCount = 0;
        _adapter.Setup(a => a.GetDetectionResultAsync("job2", _source))
            .ReturnsAsync(() => ++callCount < 3 ? InProgressDoc() : SucceededDoc());

        await CreatePoller().WaitForCompletionAsync("job2", _source);

        callCount.Should().Be(3);
    }

    [Fact]
    public async Task WaitForCompletionAsync_ThrowsTimeout_WhenMaxAttemptsExceeded()
    {
        _adapter.Setup(a => a.GetDetectionResultAsync(It.IsAny<string>(), _source))
            .ReturnsAsync(InProgressDoc());

        await CreatePoller(maxAttempts: 3).Invoking(p => p.WaitForCompletionAsync("job3", _source))
            .Should().ThrowAsync<TimeoutException>()
            .WithMessage("*job3*");
    }
}

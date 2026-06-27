using BedRock.POC.Application.Interfaces;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BedRock.POC.Infrastructure.Aws;

public class TextractJobPoller(
    ILogger<TextractJobPoller> logger,
    ITextractAdapter adapter,
    IOptions<TextractOptions> options) : ITextractJobPoller
{
    private readonly TextractOptions _opts = options.Value;

    public async Task WaitForCompletionAsync(string jobId, StorageLocation source, CancellationToken ct = default)
    {
        logger.LogInformation("Polling Textract job {JobId}", jobId);

        for (var attempt = 0; attempt < _opts.MaxPollingAttempts; attempt++)
        {
            var doc = await adapter.GetDetectionResultAsync(jobId, source);

            if (doc?.JobStatus != "IN_PROGRESS")
            {
                logger.LogInformation("Textract job {JobId} finished with status {Status}", jobId, doc?.JobStatus);
                return;
            }

            logger.LogDebug("Textract job {JobId} still in progress — attempt {Attempt}", jobId, attempt + 1);
            await Task.Delay(TimeSpan.FromSeconds(_opts.PollingIntervalSeconds), ct);
        }

        throw new TimeoutException($"Textract job {jobId} did not complete after {_opts.MaxPollingAttempts} attempts.");
    }
}

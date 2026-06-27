using BedRock.POC.Domain.ValueObjects;

namespace BedRock.POC.Application.Interfaces;

public interface ITextractJobPoller
{
    Task WaitForCompletionAsync(string jobId, StorageLocation source, CancellationToken ct = default);
}

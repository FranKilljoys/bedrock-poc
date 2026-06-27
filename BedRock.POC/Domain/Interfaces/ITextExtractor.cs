using BedRock.POC.Domain.ValueObjects;

namespace BedRock.POC.Domain.Interfaces;

public interface ITextExtractor
{
    Task<IReadOnlyList<string>> ExtractPagesAsync(StorageLocation source, CancellationToken ct = default);
}

using BedRock.POC.Domain.ValueObjects;

namespace BedRock.POC.Application.Interfaces;

public interface ITextractResultCache
{
    Task<IReadOnlyList<string>> GetCachedPagesAsync(StorageLocation source, CancellationToken ct = default);
    Task StorePagesAsync(StorageLocation source, IReadOnlyList<string> pages, CancellationToken ct = default);
}

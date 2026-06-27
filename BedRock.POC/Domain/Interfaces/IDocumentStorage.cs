using BedRock.POC.Domain.ValueObjects;

namespace BedRock.POC.Domain.Interfaces;

public interface IDocumentStorage
{
    Task<StorageLocation> UploadAsync(Stream stream, string fileName, CancellationToken ct = default);
    Task<Stream> DownloadAsync(StorageLocation location, CancellationToken ct = default);
    Task<T> DownloadJsonAsync<T>(StorageLocation location, CancellationToken ct = default);
    Task<List<StorageLocation>> ListAsync(string bucket, string prefix, CancellationToken ct = default);
    Task StoreTextAsync(StorageLocation location, string content, CancellationToken ct = default);
    Task<string> ReadTextAsync(StorageLocation location, CancellationToken ct = default);
}

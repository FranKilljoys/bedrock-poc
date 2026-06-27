namespace BedRock.POC.Domain.ValueObjects;

public record StorageLocation(string Bucket, string Key)
{
    public string FileName => Path.GetFileName(Key);

    public static StorageLocation FromUri(string s3Uri)
    {
        var path = s3Uri.StartsWith("s3://", StringComparison.OrdinalIgnoreCase)
            ? s3Uri[5..]
            : s3Uri;

        var slashIndex = path.IndexOf('/');
        if (slashIndex < 1)
            throw new ArgumentException($"Invalid S3 URI: '{s3Uri}'", nameof(s3Uri));

        return new StorageLocation(path[..slashIndex], path[(slashIndex + 1)..]);
    }

    public string ToUri() => $"s3://{Bucket}/{Key}";
}

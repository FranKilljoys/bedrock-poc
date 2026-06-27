using System.Text.Json.Serialization;

namespace BedRock.POC.Domain.Models;

public class ExtractionResult
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("startPosition")]
    public int StartPosition { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    public int PageNumber { get; set; }
}

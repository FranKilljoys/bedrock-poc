using System.Text.Json.Serialization;

namespace BedRock.POC.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BedrockFoundationModel
{
    Claude35Sonnet,
    CommandLight
}

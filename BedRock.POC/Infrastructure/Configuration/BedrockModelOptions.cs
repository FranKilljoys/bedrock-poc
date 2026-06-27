using System.ComponentModel.DataAnnotations;

namespace BedRock.POC.Infrastructure.Configuration;

public class BedrockModelOptions
{
    public const string Section = "Bedrock";

    public string DefaultPromptTemplate { get; set; }

    [Required]
    public Dictionary<string, ModelConfiguration> Models { get; set; } = new();
}

public class ModelConfiguration
{
    [Required]
    public string ModelId { get; set; }

    public float Temperature { get; set; } = 0.5f;

    public float TopP { get; set; } = 0.9f;
}

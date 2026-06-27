using BedRock.POC.Domain.Models;
using BedRock.POC.Enums;

namespace BedRock.POC.Domain.Interfaces;

public interface ILanguageModelClient
{
    BedrockFoundationModel SupportedModel { get; }
    Task<ExtractionResult[]> ConverseAsync(string[] keys, string content, string promptTemplate = null, CancellationToken ct = default);
}

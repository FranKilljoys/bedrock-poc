using BedRock.POC.Application.DTOs;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Domain.Interfaces;
using BedRock.POC.Domain.Models;
using BedRock.POC.Enums;
using BedRock.POC.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BedRock.POC.Application.UseCases;

public class ExtractDocumentUseCase(
    IDocumentStorage storage,
    ITextExtractor textExtractor,
    ILanguageModelClientFactory modelFactory,
    IOptions<ExtractionConfiguration> extractionConfig) : IExtractDocumentUseCase
{
    private readonly ExtractionConfiguration _config = extractionConfig.Value;

    public async Task<ExtractionResult[]> ExecuteAsync(ExtractDocumentRequest request, BedrockFoundationModel model, CancellationToken ct = default)
    {
        var location = await storage.UploadAsync(request.File.OpenReadStream(), request.File.FileName, ct);
        var pages = await textExtractor.ExtractPagesAsync(location, ct);

        if (pages.Count == 0) return [];

        var keys = request.Keys?.Length > 0 ? request.Keys : _config.DefaultKeys;
        var content = string.Join('\n', pages);

        var client = modelFactory.GetClient(model);
        return await client.ConverseAsync(keys, content, request.Prompt, ct) ?? [];
    }
}

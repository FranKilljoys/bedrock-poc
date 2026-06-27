using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Domain.Interfaces;
using BedRock.POC.Domain.Models;
using BedRock.POC.Enums;
using BedRock.POC.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BedRock.POC.Infrastructure.Aws.LLM;

public abstract class BedrockModelBase(
    ILogger logger,
    IAmazonBedrockRuntime bedrockClient,
    IOptions<BedrockModelOptions> options,
    IPromptBuilder promptBuilder) : ILanguageModelClient
{
    private readonly BedrockModelOptions _opts = options.Value;

    public abstract BedrockFoundationModel SupportedModel { get; }

    protected abstract string GetModelId(BedrockModelOptions opts);
    protected abstract float GetTemperature(BedrockModelOptions opts);
    protected abstract ConverseRequest BuildConverseRequest(string systemPrompt, string userContent, string modelId, float temperature);

    public async Task<ExtractionResult[]> ConverseAsync(string[] keys, string content, string promptTemplate = null, CancellationToken ct = default)
    {
        var modelId = GetModelId(_opts);
        var temperature = GetTemperature(_opts);
        var systemPrompt = promptBuilder.BuildSystemPrompt(keys, promptTemplate);
        var userContent = promptBuilder.WrapDocumentContent(content);

        var request = BuildConverseRequest(systemPrompt, userContent, modelId, temperature);

        try
        {
            var response = await bedrockClient.ConverseAsync(request, ct);
            var responseText = response?.Output?.Message?.Content?.FirstOrDefault()?.Text ?? string.Empty;

            logger.LogDebug("Bedrock response received for model {ModelId}", modelId);

            if (string.IsNullOrEmpty(responseText)) return [];

            return JsonSerializer.Deserialize<ExtractionResult[]>(responseText) ?? [];
        }
        catch (AmazonBedrockRuntimeException ex)
        {
            logger.LogError(ex, "Bedrock invocation failed for model {ModelId}", modelId);
            throw;
        }
    }
}

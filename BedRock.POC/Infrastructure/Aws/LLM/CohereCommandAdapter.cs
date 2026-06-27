using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Enums;
using BedRock.POC.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BedRock.POC.Infrastructure.Aws.LLM;

public class CohereCommandAdapter(
    ILogger<CohereCommandAdapter> logger,
    IAmazonBedrockRuntime bedrockClient,
    IOptions<BedrockModelOptions> options,
    IPromptBuilder promptBuilder)
    : BedrockModelBase(logger, bedrockClient, options, promptBuilder)
{
    public override BedrockFoundationModel SupportedModel => BedrockFoundationModel.CommandLight;

    protected override string GetModelId(BedrockModelOptions opts) =>
        opts.Models.TryGetValue(nameof(BedrockFoundationModel.CommandLight), out var cfg) ? cfg.ModelId : string.Empty;

    protected override float GetTemperature(BedrockModelOptions opts) =>
        opts.Models.TryGetValue(nameof(BedrockFoundationModel.CommandLight), out var cfg) ? cfg.Temperature : 0.7f;

    protected override ConverseRequest BuildConverseRequest(string systemPrompt, string userContent, string modelId, float temperature) =>
        new()
        {
            ModelId = modelId,
            Messages =
            [
                new Message
                {
                    Role = ConversationRole.User,
                    Content = [new ContentBlock { Text = $"{systemPrompt}\n{userContent}" }]
                }
            ],
            InferenceConfig = new InferenceConfiguration { Temperature = temperature, TopP = 0.9f }
        };
}

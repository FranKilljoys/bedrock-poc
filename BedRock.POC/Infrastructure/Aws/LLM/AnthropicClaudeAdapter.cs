using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Enums;
using BedRock.POC.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BedRock.POC.Infrastructure.Aws.LLM;

public class AnthropicClaudeAdapter(
    ILogger<AnthropicClaudeAdapter> logger,
    IAmazonBedrockRuntime bedrockClient,
    IOptions<BedrockModelOptions> options,
    IPromptBuilder promptBuilder)
    : BedrockModelBase(logger, bedrockClient, options, promptBuilder)
{
    public override BedrockFoundationModel SupportedModel => BedrockFoundationModel.Claude35Sonnet;

    protected override string GetModelId(BedrockModelOptions opts) =>
        opts.Models.TryGetValue(nameof(BedrockFoundationModel.Claude35Sonnet), out var cfg) ? cfg.ModelId : string.Empty;

    protected override float GetTemperature(BedrockModelOptions opts) =>
        opts.Models.TryGetValue(nameof(BedrockFoundationModel.Claude35Sonnet), out var cfg) ? cfg.Temperature : 0.5f;

    protected override ConverseRequest BuildConverseRequest(string systemPrompt, string userContent, string modelId, float temperature) =>
        new()
        {
            ModelId = modelId,
            System = [new SystemContentBlock { Text = systemPrompt }],
            Messages =
            [
                new Message
                {
                    Role = ConversationRole.User,
                    Content = [new ContentBlock { Text = userContent }]
                }
            ],
            InferenceConfig = new InferenceConfiguration { Temperature = temperature, TopP = 0.9f }
        };
}

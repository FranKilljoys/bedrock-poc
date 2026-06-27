using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Enums;
using BedRock.POC.Infrastructure.Aws.LLM;
using BedRock.POC.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BedRock.POC.Tests.Infrastructure;

public class CohereCommandAdapterTests
{
    private readonly Mock<IAmazonBedrockRuntime> _bedrock = new();
    private readonly Mock<IPromptBuilder> _promptBuilder = new();

    private CohereCommandAdapter CreateAdapter() => new(
        NullLogger<CohereCommandAdapter>.Instance,
        _bedrock.Object,
        Options.Create(new BedrockModelOptions
        {
            DefaultPromptTemplate = "Extract {keyList}",
            Models = new Dictionary<string, ModelConfiguration>
            {
                [nameof(BedrockFoundationModel.CommandLight)] = new() { ModelId = "cohere.command-light-text-v14", Temperature = 0.7f }
            }
        }),
        _promptBuilder.Object);

    private void SetupPromptBuilder()
    {
        _promptBuilder.Setup(p => p.BuildSystemPrompt(It.IsAny<string[]>(), It.IsAny<string>())).Returns("system prompt");
        _promptBuilder.Setup(p => p.WrapDocumentContent(It.IsAny<string>())).Returns("<document>content</document>");
    }

    private void SetupBedrockResponse(string text)
    {
        _bedrock.Setup(b => b.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConverseResponse
            {
                Output = new ConverseOutput
                {
                    Message = new Message { Content = [new ContentBlock { Text = text }] }
                }
            });
    }

    [Fact]
    public void SupportedModel_IsCommandLight()
    {
        CreateAdapter().SupportedModel.Should().Be(BedrockFoundationModel.CommandLight);
    }

    [Fact]
    public async Task ConverseAsync_ReturnsDeserializedResults()
    {
        SetupPromptBuilder();
        SetupBedrockResponse("""[{"key":"Field","value":"Value","startPosition":10,"score":88}]""");

        var result = await CreateAdapter().ConverseAsync(["Field"], "document content");

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("Field");
        result[0].Value.Should().Be("Value");
    }

    [Fact]
    public async Task ConverseAsync_BuildsRequestWithCombinedSystemAndUserContent()
    {
        SetupPromptBuilder();
        SetupBedrockResponse("[]");
        ConverseRequest capturedRequest = null;

        _bedrock.Setup(b => b.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConverseRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new ConverseResponse
            {
                Output = new ConverseOutput { Message = new Message { Content = [new ContentBlock { Text = "[]" }] } }
            });

        await CreateAdapter().ConverseAsync(["Field"], "content");

        capturedRequest.Should().NotBeNull();
        capturedRequest.System.Should().BeNullOrEmpty();
        capturedRequest.Messages[0].Content[0].Text.Should().Contain("system prompt").And.Contain("<document>content</document>");
        capturedRequest.ModelId.Should().Be("cohere.command-light-text-v14");
    }

    [Fact]
    public async Task ConverseAsync_ReturnsEmpty_WhenResponseIsEmpty()
    {
        SetupPromptBuilder();
        SetupBedrockResponse(string.Empty);

        var result = await CreateAdapter().ConverseAsync(["Field"], "content");

        result.Should().BeEmpty();
    }
}

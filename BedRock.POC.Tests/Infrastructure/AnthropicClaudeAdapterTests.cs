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

public class AnthropicClaudeAdapterTests
{
    private readonly Mock<IAmazonBedrockRuntime> _bedrock = new();
    private readonly Mock<IPromptBuilder> _promptBuilder = new();

    private AnthropicClaudeAdapter CreateAdapter() => new(
        NullLogger<AnthropicClaudeAdapter>.Instance,
        _bedrock.Object,
        Options.Create(new BedrockModelOptions
        {
            DefaultPromptTemplate = "Extract {keyList}",
            Models = new Dictionary<string, ModelConfiguration>
            {
                [nameof(BedrockFoundationModel.Claude35Sonnet)] = new() { ModelId = "anthropic.claude-3-sonnet", Temperature = 0.5f }
            }
        }),
        _promptBuilder.Object);

    private void SetupPromptBuilder()
    {
        _promptBuilder.Setup(p => p.BuildSystemPrompt(It.IsAny<string[]>(), It.IsAny<string>())).Returns("system prompt");
        _promptBuilder.Setup(p => p.WrapDocumentContent(It.IsAny<string>())).Returns("<document>content</document>");
    }

    private void SetupBedrockResponse(string responseText)
    {
        _bedrock.Setup(b => b.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConverseResponse
            {
                Output = new ConverseOutput
                {
                    Message = new Message
                    {
                        Content = [new ContentBlock { Text = responseText }]
                    }
                }
            });
    }

    [Fact]
    public void SupportedModel_IsClaudeSonnet()
    {
        CreateAdapter().SupportedModel.Should().Be(BedrockFoundationModel.Claude35Sonnet);
    }

    [Fact]
    public async Task ConverseAsync_ReturnsDeserializedResults()
    {
        SetupPromptBuilder();
        SetupBedrockResponse("""[{"key":"Name","value":"John","startPosition":0,"score":95}]""");

        var result = await CreateAdapter().ConverseAsync(["Name"], "document content");

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("Name");
        result[0].Value.Should().Be("John");
        result[0].Score.Should().Be(95);
    }

    [Fact]
    public async Task ConverseAsync_ReturnsEmptyArray_WhenResponseTextIsEmpty()
    {
        SetupPromptBuilder();
        SetupBedrockResponse(string.Empty);

        var result = await CreateAdapter().ConverseAsync(["Name"], "content");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ConverseAsync_ReturnsEmptyArray_WhenResponseIsNull()
    {
        SetupPromptBuilder();
        _bedrock.Setup(b => b.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConverseResponse { Output = new ConverseOutput { Message = new Message { Content = [] } } });

        var result = await CreateAdapter().ConverseAsync(["Name"], "content");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ConverseAsync_ThrowsAmazonException_WhenBedrockFails()
    {
        SetupPromptBuilder();
        _bedrock.Setup(b => b.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonBedrockRuntimeException("service error"));

        await CreateAdapter().Invoking(a => a.ConverseAsync(["Name"], "content"))
            .Should().ThrowAsync<AmazonBedrockRuntimeException>();
    }

    [Fact]
    public async Task ConverseAsync_BuildsRequestWithSystemBlock()
    {
        SetupPromptBuilder();
        SetupBedrockResponse("[]");
        ConverseRequest capturedRequest = null;

        _bedrock.Setup(b => b.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConverseRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new ConverseResponse { Output = new ConverseOutput { Message = new Message { Content = [new ContentBlock { Text = "[]" }] } } });

        await CreateAdapter().ConverseAsync(["Name"], "content");

        capturedRequest.Should().NotBeNull();
        capturedRequest.System.Should().NotBeEmpty();
        capturedRequest.System[0].Text.Should().Be("system prompt");
        capturedRequest.ModelId.Should().Be("anthropic.claude-3-sonnet");
    }
}

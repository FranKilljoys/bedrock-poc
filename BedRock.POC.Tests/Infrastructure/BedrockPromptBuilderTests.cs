using BedRock.POC.Infrastructure.Aws.LLM;
using BedRock.POC.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace BedRock.POC.Tests.Infrastructure;

public class BedrockPromptBuilderTests
{
    private const string DefaultTemplate = "Extract {keyList} from the document.";

    private static BedrockPromptBuilder CreateBuilder(string template = DefaultTemplate) =>
        new(Options.Create(new BedrockModelOptions { DefaultPromptTemplate = template }));

    [Fact]
    public void BuildSystemPrompt_InjectsKeyElementsIntoTemplate()
    {
        var result = CreateBuilder().BuildSystemPrompt(["First Party", "Filing Date"]);
        result.Should().Be("Extract <element>First Party</element><element>Filing Date</element> from the document.");
    }

    [Fact]
    public void BuildSystemPrompt_WithCustomTemplate_UsesCustomTemplate()
    {
        var result = CreateBuilder().BuildSystemPrompt(["Name"], "Find {keyList}.");
        result.Should().Be("Find <element>Name</element>.");
    }

    [Fact]
    public void BuildSystemPrompt_NoCustomTemplate_UsesDefault()
    {
        var result = CreateBuilder("Default: {keyList}").BuildSystemPrompt(["Key"]);
        result.Should().StartWith("Default:");
    }

    [Fact]
    public void WrapDocumentContent_WrapsInDocumentTags()
    {
        var result = CreateBuilder().WrapDocumentContent("hello world");
        result.Should().Be("<document>hello world</document>");
    }

    [Fact]
    public void BuildSystemPrompt_EmptyKeys_ProducesEmptyKeyList()
    {
        var result = CreateBuilder().BuildSystemPrompt([]);
        result.Should().Be("Extract  from the document.");
    }
}

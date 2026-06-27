using BedRock.POC.Application.Interfaces;
using BedRock.POC.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BedRock.POC.Infrastructure.Aws.LLM;

public class BedrockPromptBuilder(IOptions<BedrockModelOptions> options) : IPromptBuilder
{
    private readonly string _defaultTemplate = options.Value.DefaultPromptTemplate;

    public string BuildSystemPrompt(string[] keys, string template = null)
    {
        var keyList = string.Join("", keys.Select(k => $"<element>{k}</element>"));
        var tpl = template ?? _defaultTemplate;
        return tpl.Replace("{keyList}", keyList);
    }

    public string WrapDocumentContent(string content) => $"<document>{content}</document>";
}

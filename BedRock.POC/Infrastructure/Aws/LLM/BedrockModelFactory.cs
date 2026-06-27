using BedRock.POC.Application.Interfaces;
using BedRock.POC.Domain.Interfaces;
using BedRock.POC.Enums;

namespace BedRock.POC.Infrastructure.Aws.LLM;

public class BedrockModelFactory(IEnumerable<ILanguageModelClient> clients) : ILanguageModelClientFactory
{
    private readonly Dictionary<BedrockFoundationModel, ILanguageModelClient> _map =
        clients.ToDictionary(c => c.SupportedModel);

    public ILanguageModelClient GetClient(BedrockFoundationModel model)
    {
        if (_map.TryGetValue(model, out var client)) return client;
        throw new NotSupportedException($"No registered client for model '{model}'.");
    }
}

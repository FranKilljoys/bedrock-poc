using BedRock.POC.Domain.Interfaces;
using BedRock.POC.Enums;

namespace BedRock.POC.Application.Interfaces;

public interface ILanguageModelClientFactory
{
    ILanguageModelClient GetClient(BedrockFoundationModel model);
}

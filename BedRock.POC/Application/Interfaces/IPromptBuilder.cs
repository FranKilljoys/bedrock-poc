namespace BedRock.POC.Application.Interfaces;

public interface IPromptBuilder
{
    string BuildSystemPrompt(string[] keys, string template = null);
    string WrapDocumentContent(string content);
}

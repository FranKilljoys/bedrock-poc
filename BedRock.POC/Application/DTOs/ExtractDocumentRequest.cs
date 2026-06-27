namespace BedRock.POC.Application.DTOs;

public class ExtractDocumentRequest
{
    public IFormFile File { get; set; }
    public string[] Keys { get; set; }
    public string Prompt { get; set; }
}

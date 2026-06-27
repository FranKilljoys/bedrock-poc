using System.ComponentModel.DataAnnotations;

namespace BedRock.POC.Infrastructure.Configuration;

public class TextractOptions
{
    public const string Section = "Textract";

    [Range(1, 60)]
    public int PollingIntervalSeconds { get; set; } = 3;

    [Range(1, 300)]
    public int MaxPollingAttempts { get; set; } = 60;
}

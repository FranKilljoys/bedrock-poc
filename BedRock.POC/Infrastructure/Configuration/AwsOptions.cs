using System.ComponentModel.DataAnnotations;

namespace BedRock.POC.Infrastructure.Configuration;

public class AwsOptions
{
    public const string Section = "Aws";

    [Required]
    public string Region { get; set; }

    [Required]
    public string S3BucketName { get; set; }
}

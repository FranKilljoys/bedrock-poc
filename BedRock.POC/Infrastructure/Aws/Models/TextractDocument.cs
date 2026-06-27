using Amazon.Textract.Model;
using BedRock.POC.Domain.ValueObjects;

namespace BedRock.POC.Infrastructure.Aws.Models;

public class TextractDocument
{
    public string JobStatus { get; init; }
    public DocumentMetadata DocumentMetadata { get; init; }
    public List<Block> Blocks { get; init; } = [];
    public StorageLocation SourceFile { get; init; }
    public int TotalPageCount { get; init; }

    public static TextractDocument FromDetectionResponse(GetDocumentTextDetectionResponse response, StorageLocation source, int pageCount) =>
        new()
        {
            JobStatus = response.JobStatus,
            DocumentMetadata = response.DocumentMetadata,
            Blocks = response.Blocks ?? [],
            SourceFile = source,
            TotalPageCount = pageCount
        };

    public static TextractDocument FromAnalysisResponse(GetDocumentAnalysisResponse response, StorageLocation source, int pageCount) =>
        new()
        {
            JobStatus = response.JobStatus,
            DocumentMetadata = response.DocumentMetadata,
            Blocks = response.Blocks ?? [],
            SourceFile = source,
            TotalPageCount = pageCount
        };
}

using Amazon.Textract;
using Amazon.Textract.Model;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Infrastructure.Aws.Models;
using S3Object = Amazon.Textract.Model.S3Object;

namespace BedRock.POC.Infrastructure.Aws;

public interface ITextractAdapter
{
    Task<string> StartDetectionJobAsync(string correlationId, StorageLocation source, string snsTopicArn = null, string roleArn = null);
    Task<TextractDocument> GetDetectionResultAsync(string jobId, StorageLocation source);
}

public class TextractAdapter(ILogger<TextractAdapter> logger, IAmazonTextract client) : ITextractAdapter
{
    public async Task<string> StartDetectionJobAsync(string correlationId, StorageLocation source, string snsTopicArn = null, string roleArn = null)
    {
        logger.LogInformation("{CorrelationId}: Starting Textract detection for {Key}", correlationId, source.Key);

        NotificationChannel channel = null;
        if (snsTopicArn != null && roleArn != null)
            channel = new NotificationChannel { SNSTopicArn = snsTopicArn, RoleArn = roleArn };

        var request = new StartDocumentTextDetectionRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new S3Object { Bucket = source.Bucket, Name = source.Key }
            },
            JobTag = correlationId,
            NotificationChannel = channel
        };

        var result = await client.StartDocumentTextDetectionAsync(request);
        logger.LogInformation("{CorrelationId}: Textract job started — JobId={JobId}", correlationId, result.JobId);
        return result.JobId;
    }

    public async Task<TextractDocument> GetDetectionResultAsync(string jobId, StorageLocation source)
    {
        logger.LogInformation("Retrieving Textract results for JobId={JobId}", jobId);

        var responses = new List<GetDocumentTextDetectionResponse>();
        string nextToken = null;

        do
        {
            var req = new GetDocumentTextDetectionRequest
            {
                JobId = jobId,
                MaxResults = 1000,
                NextToken = nextToken
            };
            var resp = await client.GetDocumentTextDetectionAsync(req);
            responses.Add(resp);
            nextToken = resp.NextToken;
        }
        while (nextToken != null);

        return BuildDocument(responses, source);
    }

    private static TextractDocument BuildDocument(List<GetDocumentTextDetectionResponse> responses, StorageLocation source)
    {
        if (responses.Count == 0) return null;

        var first = responses[0];
        var allBlocks = responses.SelectMany(r => r.Blocks).ToList();
        var pageCount = responses.Max(r => r.DocumentMetadata?.Pages ?? 0);

        return TextractDocument.FromDetectionResponse(
            new GetDocumentTextDetectionResponse
            {
                JobStatus = first.JobStatus,
                DocumentMetadata = first.DocumentMetadata,
                Blocks = allBlocks
            },
            source,
            pageCount);
    }
}

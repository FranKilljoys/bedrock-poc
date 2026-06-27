# Bedrock POC

A .NET 10 Web API that extracts structured key-value data from documents using **AWS Textract** for OCR and **AWS Bedrock** large language models (Anthropic Claude, Cohere Command) for intelligent field extraction.

## What it does

1. Accepts a PDF or image file via HTTP POST
2. Uploads it to S3
3. Runs AWS Textract to extract raw text (async job with polling, S3-backed cache)
4. Sends the text to a Bedrock LLM with a configurable list of fields to find
5. Returns a structured JSON array of extracted key-value pairs with confidence scores

```json
[
  { "key": "First Party",   "value": "John Smith",      "startPosition": 42, "score": 97 },
  { "key": "Filing Date",   "value": "2024-03-15",      "startPosition": 108, "score": 91 },
  { "key": "Parcel ID",     "value": "12-34-567-890",   "startPosition": 210, "score": 88 }
]
```

## Architecture

```
API Layer          → HTTP endpoint, FluentValidation, request/response DTOs
Application Layer  → ExtractDocumentUseCase, interfaces, no framework dependencies
Infrastructure     → AWS adapters (S3, Textract, Bedrock), Polly resilience pipelines
Domain             → StorageLocation value object, ExtractionResult model, core interfaces
```

All configuration is externalised — no hardcoded regions, bucket names, model IDs, or prompt templates.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- An AWS account with:
  - S3 bucket for document storage and result caching
  - Textract enabled in your target region
  - Bedrock model access granted for `anthropic.claude-3-sonnet` and/or `cohere.command-light-text-v14`
- AWS credentials configured locally (any standard method: `~/.aws/credentials`, environment variables, or instance role)

## Getting started

### 1. Clone and restore

```bash
git clone <repo-url>
cd Bedrock.POC
dotnet restore BedRock.POC/BedRock.POC.csproj
```

### 2. Configure

Copy the template and fill in your values:

```bash
cp BedRock.POC/appsettings.json BedRock.POC/appsettings.Development.json
```

Edit `appsettings.Development.json`:

```json
{
  "Aws": {
    "Region": "us-east-1",
    "S3BucketName": "your-actual-bucket-name"
  },
  "Textract": {
    "PollingIntervalSeconds": 3,
    "MaxPollingAttempts": 60
  },
  "Bedrock": {
    "Models": {
      "Claude35Sonnet": { "ModelId": "anthropic.claude-3-sonnet-20240229-v1:0" },
      "CommandLight":   { "ModelId": "cohere.command-light-text-v14" }
    }
  }
}
```

`appsettings.Development.json` is gitignored — your real values will never be committed.

### 3. Run

```bash
dotnet run --project BedRock.POC --launch-profile http
```

Scalar API reference: [http://localhost:5015/scalar/v1](http://localhost:5015/scalar/v1)

### 4. Try it

```bash
curl -X POST http://localhost:5015/documents/Claude35Sonnet \
  -F "File=@/path/to/document.pdf"
```

Or open `BedRock.POC/BedRock.POC.http` in VS Code (REST Client extension) or JetBrains Rider for ready-made requests. Place a `sample.pdf` in the same directory as the `.http` file.

## Supported models

| Route value | Model |
|---|---|
| `Claude35Sonnet` | Anthropic Claude 3 Sonnet via AWS Bedrock |
| `CommandLight` | Cohere Command Light via AWS Bedrock |

Adding a new model requires three steps: create an adapter class extending `BedrockModelBase`, register it in DI, and add its config entry to `appsettings.json`. No factory code changes needed.

## Custom extraction keys

Pass `Keys[]` form fields to override the default key list for a single request:

```bash
curl -X POST http://localhost:5015/documents/Claude35Sonnet \
  -F "File=@document.pdf" \
  -F "Keys[0]=Grantor" \
  -F "Keys[1]=Grantee" \
  -F "Keys[2]=Legal Description"
```

## Custom prompt

Pass a `Prompt` form field with a full system prompt template. Use `{keyList}` as the placeholder where the formatted key list will be inserted.

## Build

```bash
dotnet build BedRock.POC/BedRock.POC.csproj
```

## License

MIT — see [LICENSE](LICENSE).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

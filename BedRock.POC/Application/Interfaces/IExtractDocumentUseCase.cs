using BedRock.POC.Application.DTOs;
using BedRock.POC.Domain.Models;
using BedRock.POC.Enums;

namespace BedRock.POC.Application.Interfaces;

public interface IExtractDocumentUseCase
{
    Task<ExtractionResult[]> ExecuteAsync(ExtractDocumentRequest request, BedrockFoundationModel model, CancellationToken ct = default);
}

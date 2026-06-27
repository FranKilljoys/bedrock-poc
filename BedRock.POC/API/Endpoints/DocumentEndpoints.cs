using System.Diagnostics.CodeAnalysis;
using BedRock.POC.Application.DTOs;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BedRock.POC.API.Endpoints;

[ExcludeFromCodeCoverage]
public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/documents/{model}", async (
                [FromRoute] BedrockFoundationModel model,
                [FromForm] ExtractDocumentRequest request,
                [FromServices] IValidator<ExtractDocumentRequest> validator,
                [FromServices] IExtractDocumentUseCase useCase,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                    return Results.ValidationProblem(validation.ToDictionary());

                var result = await useCase.ExecuteAsync(request, model, ct);
                return Results.Ok(result);
            })
            .WithName("ExtractDocument")
            .WithOpenApi()
            .Accepts<ExtractDocumentRequest>("multipart/form-data")
            .DisableAntiforgery();
    }
}

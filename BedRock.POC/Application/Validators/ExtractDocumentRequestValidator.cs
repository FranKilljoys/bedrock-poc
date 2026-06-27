using BedRock.POC.Application.DTOs;
using FluentValidation;

namespace BedRock.POC.Application.Validators;

public class ExtractDocumentRequestValidator : AbstractValidator<ExtractDocumentRequest>
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/tiff",
        "image/png",
        "image/jpeg"
    ];

    public ExtractDocumentRequestValidator()
    {
        RuleFor(r => r.File)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("A file is required.")
            .Must(f => f.Length > 0).WithMessage("The file must not be empty.")
            .Must(f => f.Length <= MaxFileSizeBytes).WithMessage("The file must not exceed 50 MB.")
            .Must(f => AllowedContentTypes.Contains(f.ContentType)).WithMessage(
                $"Only the following file types are accepted: {string.Join(", ", AllowedContentTypes)}.");

        RuleForEach(r => r.Keys)
            .NotEmpty().WithMessage("Each extraction key must be a non-empty string.")
            .When(r => r.Keys != null && r.Keys.Length > 0);
    }
}

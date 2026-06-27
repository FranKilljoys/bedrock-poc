using BedRock.POC.Application.DTOs;
using BedRock.POC.Application.Validators;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BedRock.POC.Tests.Application;

public class ExtractDocumentRequestValidatorTests
{
    private readonly ExtractDocumentRequestValidator _validator = new();

    private static Mock<IFormFile> MakeFile(long length = 1024, string contentType = "application/pdf")
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.ContentType).Returns(contentType);
        return mock;
    }

    [Fact]
    public async Task Validate_NullFile_FailsWithRequiredMessage()
    {
        var result = await _validator.ValidateAsync(new ExtractDocumentRequest { File = null });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "File" && e.ErrorMessage == "A file is required.");
    }

    [Fact]
    public async Task Validate_NullFile_DoesNotThrowNullReference()
    {
        var act = async () => await _validator.ValidateAsync(new ExtractDocumentRequest { File = null });
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Validate_EmptyFile_FailsWithEmptyMessage()
    {
        var result = await _validator.ValidateAsync(new ExtractDocumentRequest { File = MakeFile(0).Object });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("empty"));
    }

    [Fact]
    public async Task Validate_FileTooLarge_FailsWithSizeMessage()
    {
        var result = await _validator.ValidateAsync(new ExtractDocumentRequest { File = MakeFile(51 * 1024 * 1024).Object });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("50 MB"));
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/json")]
    [InlineData("image/gif")]
    public async Task Validate_InvalidContentType_FailsWithContentTypeMessage(string contentType)
    {
        var result = await _validator.ValidateAsync(new ExtractDocumentRequest { File = MakeFile(1024, contentType).Object });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("file types"));
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/tiff")]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    public async Task Validate_ValidContentType_PassesFileValidation(string contentType)
    {
        var result = await _validator.ValidateAsync(new ExtractDocumentRequest { File = MakeFile(1024, contentType).Object });

        result.Errors.Should().NotContain(e => e.PropertyName == "File");
    }

    [Fact]
    public async Task Validate_EmptyStringInKeys_FailsKeyValidation()
    {
        var result = await _validator.ValidateAsync(new ExtractDocumentRequest
        {
            File = MakeFile().Object,
            Keys = ["ValidKey", ""]
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith("Keys"));
    }

    [Fact]
    public async Task Validate_NullKeys_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new ExtractDocumentRequest
        {
            File = MakeFile().Object,
            Keys = null
        });

        result.Errors.Should().NotContain(e => e.PropertyName.StartsWith("Keys"));
    }

    [Fact]
    public async Task Validate_ValidRequest_IsValid()
    {
        var result = await _validator.ValidateAsync(new ExtractDocumentRequest
        {
            File = MakeFile().Object,
            Keys = ["First Party", "Filing Date"]
        });

        result.IsValid.Should().BeTrue();
    }
}

using BedRock.POC.Application.DTOs;
using BedRock.POC.Application.UseCases;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Domain.Interfaces;
using BedRock.POC.Domain.Models;
using BedRock.POC.Domain.ValueObjects;
using BedRock.POC.Enums;
using BedRock.POC.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace BedRock.POC.Tests.Application;

public class ExtractDocumentUseCaseTests
{
    private readonly Mock<IDocumentStorage> _storage = new();
    private readonly Mock<ITextExtractor> _extractor = new();
    private readonly Mock<ILanguageModelClientFactory> _factory = new();
    private readonly Mock<ILanguageModelClient> _client = new();
    private readonly StorageLocation _location = new("bucket", "docs/file.pdf");

    private ExtractDocumentUseCase CreateUseCase(string[] defaultKeys = null) =>
        new(_storage.Object, _extractor.Object, _factory.Object,
            Options.Create(new ExtractionConfiguration { DefaultKeys = defaultKeys ?? ["Default Key"] }));

    private static IFormFile MockFile(string name = "file.pdf")
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(name);
        mock.Setup(f => f.OpenReadStream()).Returns(Stream.Null);
        return mock.Object;
    }

    [Fact]
    public async Task ExecuteAsync_WhenPagesAreEmpty_ReturnsEmptyArray()
    {
        _storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_location);
        _extractor.Setup(e => e.ExtractPagesAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateUseCase().ExecuteAsync(
            new ExtractDocumentRequest { File = MockFile() },
            BedrockFoundationModel.Claude35Sonnet);

        result.Should().BeEmpty();
        _factory.Verify(f => f.GetClient(It.IsAny<BedrockFoundationModel>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestHasNoKeys_UsesDefaultKeys()
    {
        var defaultKeys = new[] { "Key A", "Key B" };
        string[] capturedKeys = null;

        _storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_location);
        _extractor.Setup(e => e.ExtractPagesAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["page content"]);
        _factory.Setup(f => f.GetClient(BedrockFoundationModel.Claude35Sonnet)).Returns(_client.Object);
        _client.Setup(c => c.ConverseAsync(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string[], string, string, CancellationToken>((keys, _, _, _) => capturedKeys = keys)
            .ReturnsAsync([]);

        await CreateUseCase(defaultKeys).ExecuteAsync(
            new ExtractDocumentRequest { File = MockFile(), Keys = null },
            BedrockFoundationModel.Claude35Sonnet);

        capturedKeys.Should().BeEquivalentTo(defaultKeys);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestHasKeys_UsesRequestKeys()
    {
        var requestKeys = new[] { "Custom Key 1", "Custom Key 2" };
        string[] capturedKeys = null;

        _storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_location);
        _extractor.Setup(e => e.ExtractPagesAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["page content"]);
        _factory.Setup(f => f.GetClient(BedrockFoundationModel.Claude35Sonnet)).Returns(_client.Object);
        _client.Setup(c => c.ConverseAsync(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string[], string, string, CancellationToken>((keys, _, _, _) => capturedKeys = keys)
            .ReturnsAsync([]);

        await CreateUseCase().ExecuteAsync(
            new ExtractDocumentRequest { File = MockFile(), Keys = requestKeys },
            BedrockFoundationModel.Claude35Sonnet);

        capturedKeys.Should().BeEquivalentTo(requestKeys);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsResultsFromLlmClient()
    {
        var expected = new[] { new ExtractionResult { Key = "Name", Value = "John" } };

        _storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_location);
        _extractor.Setup(e => e.ExtractPagesAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["content"]);
        _factory.Setup(f => f.GetClient(BedrockFoundationModel.Claude35Sonnet)).Returns(_client.Object);
        _client.Setup(c => c.ConverseAsync(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateUseCase().ExecuteAsync(
            new ExtractDocumentRequest { File = MockFile() },
            BedrockFoundationModel.Claude35Sonnet);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ExecuteAsync_UploadsFileBeforeExtracting()
    {
        var callOrder = new List<string>();

        _storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("upload"))
            .ReturnsAsync(_location);
        _extractor.Setup(e => e.ExtractPagesAsync(_location, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("extract"))
            .ReturnsAsync([]);

        await CreateUseCase().ExecuteAsync(
            new ExtractDocumentRequest { File = MockFile() },
            BedrockFoundationModel.Claude35Sonnet);

        callOrder.Should().ContainInOrder("upload", "extract");
    }
}

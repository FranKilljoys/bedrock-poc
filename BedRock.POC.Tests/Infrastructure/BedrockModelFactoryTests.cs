using BedRock.POC.Domain.Interfaces;
using BedRock.POC.Enums;
using BedRock.POC.Infrastructure.Aws.LLM;
using FluentAssertions;
using Moq;

namespace BedRock.POC.Tests.Infrastructure;

public class BedrockModelFactoryTests
{
    [Fact]
    public void GetClient_RegisteredModel_ReturnsMatchingClient()
    {
        var mock = new Mock<ILanguageModelClient>();
        mock.Setup(c => c.SupportedModel).Returns(BedrockFoundationModel.Claude35Sonnet);

        var factory = new BedrockModelFactory([mock.Object]);

        factory.GetClient(BedrockFoundationModel.Claude35Sonnet).Should().BeSameAs(mock.Object);
    }

    [Fact]
    public void GetClient_MultipleClients_ReturnsCorrectOne()
    {
        var claude = new Mock<ILanguageModelClient>();
        claude.Setup(c => c.SupportedModel).Returns(BedrockFoundationModel.Claude35Sonnet);

        var cohere = new Mock<ILanguageModelClient>();
        cohere.Setup(c => c.SupportedModel).Returns(BedrockFoundationModel.CommandLight);

        var factory = new BedrockModelFactory([claude.Object, cohere.Object]);

        factory.GetClient(BedrockFoundationModel.CommandLight).Should().BeSameAs(cohere.Object);
    }

    [Fact]
    public void GetClient_UnregisteredModel_ThrowsNotSupportedException()
    {
        var factory = new BedrockModelFactory([]);

        var act = () => factory.GetClient(BedrockFoundationModel.Claude35Sonnet);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Claude35Sonnet*");
    }
}

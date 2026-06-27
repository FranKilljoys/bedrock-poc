using Amazon.Textract;
using Amazon.Textract.Model;
using BedRock.POC.Infrastructure.Aws.Models;
using FluentAssertions;

namespace BedRock.POC.Tests.Infrastructure;

public class TextractDocumentParserTests
{
    [Fact]
    public void ExtractPages_GroupsLinesByPage()
    {
        var blocks = new List<Block>
        {
            new() { BlockType = BlockType.LINE, Text = "Page 1 line 1", Page = 1 },
            new() { BlockType = BlockType.LINE, Text = "Page 2 line 1", Page = 2 },
            new() { BlockType = BlockType.LINE, Text = "Page 1 line 2", Page = 1 },
        };

        var pages = TextractDocumentParser.ExtractPages(blocks);

        pages.Should().HaveCount(2);
        pages[0].Should().Contain("Page 1 line 1").And.Contain("Page 1 line 2");
        pages[1].Should().Be("Page 2 line 1");
    }

    [Fact]
    public void ExtractPages_IgnoresNonLineBlocks()
    {
        var blocks = new List<Block>
        {
            new() { BlockType = BlockType.WORD, Text = "ignored", Page = 1 },
            new() { BlockType = BlockType.PAGE, Text = "also ignored", Page = 1 },
            new() { BlockType = BlockType.LINE, Text = "kept", Page = 1 },
        };

        var pages = TextractDocumentParser.ExtractPages(blocks);

        pages.Should().HaveCount(1);
        pages[0].Should().Be("kept");
    }

    [Fact]
    public void ExtractPages_EmptyInput_ReturnsEmptyList()
    {
        TextractDocumentParser.ExtractPages([]).Should().BeEmpty();
    }

    [Fact]
    public void ExtractPages_NullPage_DefaultsToPageOne()
    {
        var blocks = new List<Block>
        {
            new() { BlockType = BlockType.LINE, Text = "no page", Page = null },
        };

        var pages = TextractDocumentParser.ExtractPages(blocks);

        pages.Should().HaveCount(1);
        pages[0].Should().Be("no page");
    }

    [Fact]
    public void ExtractPages_PagesReturnedInOrder()
    {
        var blocks = new List<Block>
        {
            new() { BlockType = BlockType.LINE, Text = "page 3", Page = 3 },
            new() { BlockType = BlockType.LINE, Text = "page 1", Page = 1 },
            new() { BlockType = BlockType.LINE, Text = "page 2", Page = 2 },
        };

        var pages = TextractDocumentParser.ExtractPages(blocks);

        pages[0].Should().Be("page 1");
        pages[1].Should().Be("page 2");
        pages[2].Should().Be("page 3");
    }
}

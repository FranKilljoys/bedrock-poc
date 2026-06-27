using BedRock.POC.Domain.ValueObjects;
using FluentAssertions;

namespace BedRock.POC.Tests.Domain;

public class StorageLocationTests
{
    [Fact]
    public void FromUri_WithS3Prefix_ParsesBucketAndKey()
    {
        var loc = StorageLocation.FromUri("s3://my-bucket/folder/file.pdf");
        loc.Bucket.Should().Be("my-bucket");
        loc.Key.Should().Be("folder/file.pdf");
    }

    [Fact]
    public void FromUri_WithoutS3Prefix_ParsesBucketAndKey()
    {
        var loc = StorageLocation.FromUri("my-bucket/file.pdf");
        loc.Bucket.Should().Be("my-bucket");
        loc.Key.Should().Be("file.pdf");
    }

    [Theory]
    [InlineData("s3://")]
    [InlineData("s3://bucket")]
    [InlineData("justabucket")]
    public void FromUri_InvalidUri_Throws(string uri)
    {
        var act = () => StorageLocation.FromUri(uri);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToUri_ReturnsS3Uri()
    {
        var loc = new StorageLocation("my-bucket", "folder/file.pdf");
        loc.ToUri().Should().Be("s3://my-bucket/folder/file.pdf");
    }

    [Fact]
    public void FileName_ExtractsFilenameFromKey()
    {
        var loc = new StorageLocation("bucket", "folder/sub/document.pdf");
        loc.FileName.Should().Be("document.pdf");
    }

    [Fact]
    public void Equality_SameBucketAndKey_AreEqual()
    {
        var a = new StorageLocation("bucket", "key.pdf");
        var b = new StorageLocation("bucket", "key.pdf");
        a.Should().Be(b);
    }
}

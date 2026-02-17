using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

public class SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_GlobPattern_ShouldMatchFiles()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/root/app.cs", new MockFileData("class A {}") },
            { "/root/readme.md", new MockFileData("docs") },
            { "/root/src/lib.cs", new MockFileData("class B {}") },
            { "/root/src/data.json", new MockFileData("{}")}
        });

        var service = new SearchService(mockFileSystem);

        // Act
        var results = new List<string>();
        await foreach (var item in service.SearchAsync("/root", "**/*.cs"))
        {
            results.Add(item.FullPath);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains("/root/app.cs", results);
        Assert.Contains("/root/src/lib.cs", results);
    }

    [Fact]
    public async Task SearchAsync_SingleLevelPattern_ShouldMatchTopLevelOnly()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/root/app.cs", new MockFileData("class A {}") },
            { "/root/src/lib.cs", new MockFileData("class B {}") }
        });

        var service = new SearchService(mockFileSystem);

        // Act
        var results = new List<string>();
        await foreach (var item in service.SearchAsync("/root", "*.cs"))
        {
            results.Add(item.FullPath);
        }

        // Assert
        Assert.Single(results);
        Assert.Contains("/root/app.cs", results);
    }

    [Fact]
    public async Task SearchAsync_DirectoryPattern_ShouldMatchDirectories()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/root/src/app.cs", new MockFileData("class A {}") },
            { "/root/docs/readme.md", new MockFileData("docs") }
        });

        var service = new SearchService(mockFileSystem);

        // Act
        var results = new List<string>();
        await foreach (var item in service.SearchAsync("/root", "**/docs"))
        {
            results.Add(item.FullPath);
        }

        // Assert
        Assert.Single(results);
        Assert.Contains("/root/docs", results);
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ShouldReturnEmpty()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/root/app.cs", new MockFileData("class A {}") }
        });

        var service = new SearchService(mockFileSystem);

        // Act
        var results = new List<string>();
        await foreach (var item in service.SearchAsync("/root", "**/*.md"))
        {
            results.Add(item.FullPath);
        }

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_CancelledToken_ShouldThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/root/app.cs", new MockFileData("class A {}") }
        });

        var service = new SearchService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in service.SearchAsync("/root", "**/*.cs", cts.Token))
            {
            }
        });
    }
}

using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

public class StressTestingTests
{
    [Fact]
    public async Task SearchService_DeepDirectory_ShouldFindDeepFile()
    {
        // Arrange
        var root = "/root";
        var current = root;

        for (var i = 1; i <= 12; i++)
        {
            current = Path.Combine(current, $"level{i}");
        }

        var deepFile = Path.Combine(current, "deep.txt");
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { deepFile, new MockFileData("deep content") }
        });

        var service = new SearchService(mockFileSystem);

        // Act
        var results = new List<string>();
        await foreach (var item in service.SearchAsync(root, "**/*.txt"))
        {
            results.Add(item.FullPath);
        }

        // Assert
        Assert.Single(results);
        Assert.Contains(deepFile, results);
    }

    [Fact]
    public void PathResolver_DeepDirectory_ShouldResolvePath()
    {
        // Arrange
        var root = "/root";
        var current = root;

        for (var i = 1; i <= 12; i++)
        {
            current = Path.Combine(current, $"level{i}");
        }

        var deepFile = Path.Combine(current, "deep.txt");
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { deepFile, new MockFileData("deep content") }
        });

        var resolver = new PathResolver(mockFileSystem);

        // Act
        var resolved = resolver.ResolvePath(deepFile);

        // Assert
        var expected = mockFileSystem.Path.GetFullPath(deepFile);
        Assert.Equal(expected, resolved);
    }
}

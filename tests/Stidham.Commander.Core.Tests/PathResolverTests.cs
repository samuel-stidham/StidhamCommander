using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Exceptions;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

public class PathResolverTests
{
    [Fact]
    public void ResolvePath_TildeOnly_ShouldExpandToHome()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var resolver = new PathResolver(mockFileSystem);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var resolved = resolver.ResolvePath("~");

        // Assert
        Assert.Equal(home, resolved);
    }

    [Fact]
    public void ResolvePath_TildeWithSubPath_ShouldCombineWithHome()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var resolver = new PathResolver(mockFileSystem);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var resolved = resolver.ResolvePath("~/docs/file.txt");

        // Assert
        Assert.Equal(mockFileSystem.Path.Combine(home, "docs", "file.txt"), resolved);
    }

    [Fact]
    public void ResolvePath_NormalizesDotSegments()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var resolver = new PathResolver(mockFileSystem);
        var input = "/home/user/../user/docs/./file.txt";

        // Act
        var resolved = resolver.ResolvePath(input);

        // Assert
        var expected = mockFileSystem.Path.GetFullPath(input);
        Assert.Equal(expected, resolved);
    }

    [Fact]
    public void ResolvePath_UnknownTildeUser_ShouldNotExpand()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var resolver = new PathResolver(mockFileSystem);
        var input = "~otheruser/docs";

        // Act
        var resolved = resolver.ResolvePath(input);

        // Assert
        Assert.Equal(mockFileSystem.Path.GetFullPath(input), resolved);
    }

    [Fact]
    public void ResolvePath_CircularSymlink_ShouldThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var symlinkMap = new Dictionary<string, string?>
        {
            ["/a"] = "/b",
            ["/b"] = "/a"
        };

        var resolver = new PathResolver(mockFileSystem, path =>
            symlinkMap.TryGetValue(path, out var target) ? target : null);

        // Act & Assert
        var ex = Assert.Throws<CircularSymlinkException>(() => resolver.ResolvePath("/a"));
        Assert.Equal("ResolvePath", ex.OperationName);
    }

    [Fact]
    public void ResolvePath_SymlinkChain_ShouldResolveToFinalTarget()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var symlinkMap = new Dictionary<string, string?>
        {
            ["/link"] = "/target",
            ["/target"] = null
        };

        var resolver = new PathResolver(mockFileSystem, path =>
            symlinkMap.TryGetValue(path, out var target) ? target : null);

        // Act
        var resolved = resolver.ResolvePath("/link");

        // Assert
        Assert.Equal("/target", resolved);
    }
}

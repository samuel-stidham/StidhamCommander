using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

public class FileDiscoveryTests
{
    [Fact]
    public void GetItems_ShouldReturnMockedContents()
    {
        // Arrange: Create a fake file system in memory
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"/home/samuel/test.txt", new MockFileData("Testing 123") },
            { @"/home/samuel/projects", new MockDirectoryData() }
        });

        var service = new FileDiscoveryService(mockFileSystem);

        // Act
        var results = service.GetItems("/home/samuel").ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "projects" && r.IsDirectory);
        Assert.Contains(results, r => r.Name == "test.txt" && !r.IsDirectory);
    }
}

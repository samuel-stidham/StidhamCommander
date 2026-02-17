using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Exceptions;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Tests custom exception handling in FileOperationService.
/// </summary>
public class FileOperationServiceExceptionTests
{
    [Fact]
    public async Task DeleteAsync_ProtectedPath_ShouldThrowProtectedPathException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ProtectedPathException>(() =>
            service.DeleteAsync("/etc"));

        Assert.Equal("/etc", ex.Path);
        Assert.Equal("Delete", ex.OperationName);
        Assert.IsAssignableFrom<UnauthorizedAccessException>(ex);
    }

    [Fact]
    public async Task CopyAsync_ProtectedSourcePath_ShouldThrowProtectedPathException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ProtectedPathException>(() =>
            service.CopyAsync("/bin", "/dest"));

        Assert.Equal("/bin", ex.Path);
        Assert.Equal("Copy", ex.OperationName);
    }

    [Fact]
    public async Task MoveAsync_ProtectedDestinationPath_ShouldThrowProtectedPathException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("/source");
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ProtectedPathException>(() =>
            service.MoveAsync("/source", "/etc"));

        Assert.Equal("/etc", ex.Path);
        Assert.Equal("Move", ex.OperationName);
    }

    [Fact]
    public async Task RenameAsync_NonExistentPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.RenameAsync("/nonexistent", "/newname"));
    }

    [Fact]
    public async Task CopyAsync_NonExistentSource_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.CopyAsync("/nonexistent", "/dest"));
    }

    [Fact]
    public async Task MoveAsync_NonExistentSource_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.MoveAsync("/nonexistent", "/dest"));
    }

    [Fact]
    public async Task CopyAsync_ExistingDestination_ShouldThrowIOException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/source.txt", new MockFileData("content") },
            { "/dest.txt", new MockFileData("existing") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert - overwrite=false should throw IOException
        await Assert.ThrowsAsync<IOException>(() =>
            service.CopyAsync("/source.txt", "/dest.txt", overwrite: false));
    }
}

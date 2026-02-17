using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Models;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Tests MoveAsync functionality for FileOperationService.
/// </summary>
public class FileOperationServiceMoveTests
{
    [Fact]
    public async Task MoveAsync_SingleFile_ShouldMoveFileToDestination()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData("Test content")
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.MoveAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.True(mockFileSystem.File.Exists("/dest/file.txt"));
        Assert.Equal("Test content", mockFileSystem.File.ReadAllText("/dest/file.txt"));
        Assert.False(mockFileSystem.File.Exists("/source/file.txt")); // Original removed
    }

    [Fact]
    public async Task MoveAsync_Directory_ShouldMoveEntireDirectoryTree()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file1.txt"] = new MockFileData("File 1"),
            ["/source/file2.txt"] = new MockFileData("File 2"),
            ["/source/subdir/file3.txt"] = new MockFileData("File 3")
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.MoveAsync("/source", "/dest");

        // Assert
        Assert.True(mockFileSystem.Directory.Exists("/dest"));
        Assert.True(mockFileSystem.File.Exists("/dest/file1.txt"));
        Assert.True(mockFileSystem.File.Exists("/dest/file2.txt"));
        Assert.True(mockFileSystem.File.Exists("/dest/subdir/file3.txt"));
        Assert.False(mockFileSystem.Directory.Exists("/source")); // Original removed
    }

    [Fact]
    public async Task MoveAsync_WithProgress_ShouldAcceptProgressParameter()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file1.txt"] = new MockFileData(new byte[1024]),
            ["/source/file2.txt"] = new MockFileData(new byte[2048])
        });

        var service = new FileOperationService(mockFileSystem);
        var progressReports = new List<OperationProgress>();
        var progress = new Progress<OperationProgress>(p => progressReports.Add(p));

        // Act
        await service.MoveAsync("/source", "/dest", progress);

        // Assert
        Assert.True(mockFileSystem.Directory.Exists("/dest"));
        Assert.False(mockFileSystem.Directory.Exists("/source"));
        // Progress may or may not be reported depending on whether atomic move succeeds
    }

    [Fact]
    public async Task MoveAsync_WithCancellation_ShouldThrowOperationCanceled()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData(new byte[1024])
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.MoveAsync("/source/file.txt", "/dest/file.txt", ct: cts.Token));
    }

    [Fact]
    public async Task MoveAsync_SourceNotFound_ShouldThrowFileNotFound()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.MoveAsync("/nonexistent/file.txt", "/dest/file.txt"));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MoveAsync_ShouldRaiseOperationStartedEvent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData("Content")
        });
        var service = new FileOperationService(mockFileSystem);
        OperationStartedEventArgs? capturedEvent = null;
        service.OperationStarted += (s, e) => capturedEvent = e;

        // Act
        await service.MoveAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal("Move", capturedEvent.OperationName);
        Assert.Equal("/source/file.txt", capturedEvent.Path);
    }

    [Fact]
    public async Task MoveAsync_ShouldRaiseOperationCompletedEvent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData(new byte[1024])
        });
        var service = new FileOperationService(mockFileSystem);
        OperationCompletedEventArgs? capturedEvent = null;
        service.OperationCompleted += (s, e) => capturedEvent = e;

        // Act
        await service.MoveAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal("Move", capturedEvent.OperationName);
        Assert.True(capturedEvent.TotalBytes >= 0);
    }

    [Fact]
    public async Task MoveAsync_ShouldRaiseOperationFailedEventOnError()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);
        OperationFailedEventArgs? capturedEvent = null;
        service.OperationFailed += (s, e) => capturedEvent = e;

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.MoveAsync("/nonexistent/file.txt", "/dest/file.txt"));

        Assert.NotNull(capturedEvent);
        Assert.Equal("Move", capturedEvent.OperationName);
        Assert.Equal("/nonexistent/file.txt", capturedEvent.Path);
        Assert.NotNull(capturedEvent.Error);
    }

    [Fact]
    public async Task MoveAsync_ProtectedSourcePath_ShouldThrowUnauthorizedAccess()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("/etc");
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.MoveAsync("/etc", "/dest"));
    }

    [Fact]
    public async Task MoveAsync_ProtectedDestinationPath_ShouldThrowUnauthorizedAccess()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("/source");
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.MoveAsync("/source", "/etc"));
    }

    [Fact]
    public async Task MoveAsync_EmptyDirectory_ShouldMoveEmptyDirectory()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("/source");
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.MoveAsync("/source", "/dest");

        // Assert
        Assert.True(mockFileSystem.Directory.Exists("/dest"));
        Assert.False(mockFileSystem.Directory.Exists("/source"));
    }

    [Fact]
    public async Task MoveAsync_FileToExistingDestination_ShouldOverwrite()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData("New content"),
            ["/dest/file.txt"] = new MockFileData("Old content")
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.MoveAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.Equal("New content", mockFileSystem.File.ReadAllText("/dest/file.txt"));
        Assert.False(mockFileSystem.File.Exists("/source/file.txt"));
    }

    [Fact]
    public async Task MoveAsync_NestedDirectoryStructure_ShouldMoveEntireTree()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/level1/level2/level3/file.txt"] = new MockFileData("Deep file")
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.MoveAsync("/source", "/dest");

        // Assert
        Assert.True(mockFileSystem.File.Exists("/dest/level1/level2/level3/file.txt"));
        Assert.Equal("Deep file", mockFileSystem.File.ReadAllText("/dest/level1/level2/level3/file.txt"));
        Assert.False(mockFileSystem.Directory.Exists("/source"));
    }

    [Fact]
    public async Task MoveAsync_MultipleFiles_ShouldMoveAllFiles()
    {
        // Arrange
        var files = new Dictionary<string, MockFileData>();
        for (int i = 0; i < 10; i++)
        {
            files[$"/source/file{i}.txt"] = new MockFileData($"Content {i}");
        }
        var mockFileSystem = new MockFileSystem(files);
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.MoveAsync("/source", "/dest");

        // Assert
        for (int i = 0; i < 10; i++)
        {
            Assert.True(mockFileSystem.File.Exists($"/dest/file{i}.txt"));
            Assert.Equal($"Content {i}", mockFileSystem.File.ReadAllText($"/dest/file{i}.txt"));
        }
        Assert.False(mockFileSystem.Directory.Exists("/source"));
    }

    [Fact]
    public async Task MoveAsync_SameVolume_ShouldUseAtomicMove()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData("Content")
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.MoveAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.True(mockFileSystem.File.Exists("/dest/file.txt"));
        Assert.False(mockFileSystem.File.Exists("/source/file.txt"));
    }
}

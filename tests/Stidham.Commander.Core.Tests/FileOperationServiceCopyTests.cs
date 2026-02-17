using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Models;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Tests CopyAsync functionality for FileOperationService.
/// </summary>
public class FileOperationServiceCopyTests
{
    [Fact]
    public async Task CopyAsync_SingleFile_ShouldCopyFileToDestination()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData("Test content")
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.CopyAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.True(mockFileSystem.File.Exists("/dest/file.txt"));
        Assert.Equal("Test content", mockFileSystem.File.ReadAllText("/dest/file.txt"));
        Assert.True(mockFileSystem.File.Exists("/source/file.txt")); // Original still exists
    }

    [Fact]
    public async Task CopyAsync_DirectoryTree_ShouldCopyAllFilesAndSubdirectories()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file1.txt"] = new MockFileData("File 1"),
            ["/source/file2.txt"] = new MockFileData("File 2"),
            ["/source/subdir/file3.txt"] = new MockFileData("File 3"),
            ["/source/subdir/nested/file4.txt"] = new MockFileData("File 4")
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.CopyAsync("/source", "/dest");

        // Assert
        Assert.True(mockFileSystem.File.Exists("/dest/file1.txt"));
        Assert.True(mockFileSystem.File.Exists("/dest/file2.txt"));
        Assert.True(mockFileSystem.File.Exists("/dest/subdir/file3.txt"));
        Assert.True(mockFileSystem.File.Exists("/dest/subdir/nested/file4.txt"));
        Assert.Equal("File 1", mockFileSystem.File.ReadAllText("/dest/file1.txt"));
        Assert.Equal("File 4", mockFileSystem.File.ReadAllText("/dest/subdir/nested/file4.txt"));
    }

    [Fact]
    public async Task CopyAsync_WithProgress_ShouldReportProgressThroughIProgress()
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
        await service.CopyAsync("/source", "/dest", progress: progress);

        // Assert
        Assert.NotEmpty(progressReports);
        Assert.All(progressReports, p => Assert.Equal("Copy", p.OperationName));
        Assert.True(progressReports.Last().BytesProcessed >= 3072); // Total size
    }

    [Fact]
    public async Task CopyAsync_WithCancellation_ShouldThrowOperationCanceled()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file1.txt"] = new MockFileData(new byte[1024]),
            ["/source/file2.txt"] = new MockFileData(new byte[2048])
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CopyAsync("/source", "/dest", ct: cts.Token));
    }

    [Fact]
    public async Task CopyAsync_OverwriteTrue_ShouldOverwriteExistingFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData("New content"),
            ["/dest/file.txt"] = new MockFileData("Old content")
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.CopyAsync("/source/file.txt", "/dest/file.txt", overwrite: true);

        // Assert
        Assert.Equal("New content", mockFileSystem.File.ReadAllText("/dest/file.txt"));
    }

    [Fact]
    public async Task CopyAsync_OverwriteFalse_ShouldThrowWhenDestinationExists()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData("New content"),
            ["/dest/file.txt"] = new MockFileData("Old content")
        });
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<IOException>(() =>
            service.CopyAsync("/source/file.txt", "/dest/file.txt", overwrite: false));

        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CopyAsync_SourceNotFound_ShouldThrowFileNotFound()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.CopyAsync("/nonexistent/file.txt", "/dest/file.txt"));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CopyAsync_ShouldRaiseOperationStartedEvent()
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
        await service.CopyAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal("Copy", capturedEvent.OperationName);
        Assert.Equal("/source/file.txt", capturedEvent.Path);
    }

    [Fact]
    public async Task CopyAsync_ShouldRaiseOperationCompletedEvent()
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
        await service.CopyAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal("Copy", capturedEvent.OperationName);
        Assert.Equal(1024, capturedEvent.TotalBytes);
    }

    [Fact]
    public async Task CopyAsync_ShouldRaiseOperationProgressEvent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file1.txt"] = new MockFileData(new byte[1024]),
            ["/source/file2.txt"] = new MockFileData(new byte[2048])
        });
        var service = new FileOperationService(mockFileSystem);
        var progressEvents = new List<OperationProgressEventArgs>();
        service.OperationProgress += (s, e) => progressEvents.Add(e);

        // Act
        await service.CopyAsync("/source", "/dest");

        // Assert
        Assert.NotEmpty(progressEvents);
        Assert.True(progressEvents.Last().BytesProcessed >= 3072);
        Assert.Equal(3072, progressEvents.Last().TotalBytes);
    }

    [Fact]
    public async Task CopyAsync_ShouldRaiseOperationFailedEventOnError()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);
        OperationFailedEventArgs? capturedEvent = null;
        service.OperationFailed += (s, e) => capturedEvent = e;

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.CopyAsync("/nonexistent/file.txt", "/dest/file.txt"));

        Assert.NotNull(capturedEvent);
        Assert.Equal("Copy", capturedEvent.OperationName);
        Assert.Equal("/nonexistent/file.txt", capturedEvent.Path);
        Assert.NotNull(capturedEvent.Error);
    }

    [Fact]
    public async Task CopyAsync_EmptyDirectory_ShouldCreateEmptyDestinationDirectory()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("/source");
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.CopyAsync("/source", "/dest");

        // Assert
        Assert.True(mockFileSystem.Directory.Exists("/dest"));
    }

    [Fact]
    public async Task CopyAsync_ProtectedSourcePath_ShouldThrowUnauthorizedAccess()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("/etc");
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CopyAsync("/etc", "/dest"));
    }

    [Fact]
    public async Task CopyAsync_ProtectedDestinationPath_ShouldThrowUnauthorizedAccess()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("/source");
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CopyAsync("/source", "/etc"));
    }

    [Fact]
    public async Task CopyAsync_DirectoryWithManyFiles_ShouldReportAccurateProgress()
    {
        // Arrange
        var files = new Dictionary<string, MockFileData>();
        for (int i = 0; i < 10; i++)
        {
            files[$"/source/file{i}.txt"] = new MockFileData(new byte[100]);
        }
        var mockFileSystem = new MockFileSystem(files);
        var service = new FileOperationService(mockFileSystem);
        var progressReports = new List<OperationProgress>();
        var progress = new Progress<OperationProgress>(p => progressReports.Add(p));

        // Act
        await service.CopyAsync("/source", "/dest", progress: progress);

        // Assert
        Assert.NotEmpty(progressReports);
        var finalProgress = progressReports.Last();
        Assert.Equal(1000, finalProgress.BytesProcessed);
        Assert.Equal(1000, finalProgress.TotalBytes);
        Assert.Equal(100.0, finalProgress.PercentComplete);
    }
}

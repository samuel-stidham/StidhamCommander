using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Models;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Comprehensive cancellation tests verifying that all operations respect CancellationToken.
/// </summary>
public class FileOperationServiceCancellationTests
{
    [Fact]
    public async Task DeleteAsync_ImmediateCancellation_ShouldThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/test/file.txt"] = new MockFileData("content")
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before operation starts

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.DeleteAsync("/test/file.txt", ct: cts.Token));
    }

    [Fact]
    public async Task RenameAsync_ImmediateCancellation_ShouldThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/test/file.txt"] = new MockFileData("content")
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RenameAsync("/test/file.txt", "/test/renamed.txt", ct: cts.Token));
    }

    [Fact]
    public async Task CopyAsync_ImmediateCancellation_ShouldThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData("content")
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CopyAsync("/source/file.txt", "/dest/file.txt", ct: cts.Token));
    }

    [Fact]
    public async Task MoveAsync_ImmediateCancellation_ShouldThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file.txt"] = new MockFileData("content")
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.MoveAsync("/source/file.txt", "/dest/file.txt", ct: cts.Token));
    }

    [Fact]
    public async Task CopyAsync_DirectoryWithMultipleFiles_CancellationDuringOperation()
    {
        // Arrange - Create many files to increase chance of catching cancellation
        var files = new Dictionary<string, MockFileData>();
        for (int i = 0; i < 100; i++)
        {
            files[$"/source/file{i}.txt"] = new MockFileData(new byte[1024]);
        }
        var mockFileSystem = new MockFileSystem(files);
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();

        // Track progress and cancel after first file
        var progressCount = 0;
        var progress = new Progress<OperationProgress>(p =>
        {
            progressCount++;
            if (progressCount == 1)
            {
                cts.Cancel(); // Cancel after first progress report
            }
        });

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CopyAsync("/source", "/dest", progress: progress, ct: cts.Token));

        // Verify operation was interrupted (not all files copied)
        var copiedFiles = mockFileSystem.Directory.GetFiles("/dest", "*", SearchOption.AllDirectories).Length;
        Assert.True(copiedFiles < 100, $"Expected fewer than 100 files copied due to cancellation, but got {copiedFiles}");
    }

    [Fact]
    public async Task DeleteAsync_RecursiveDirectory_CancellationCheck()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/test/file1.txt"] = new MockFileData("content"),
            ["/test/file2.txt"] = new MockFileData("content"),
            ["/test/subdir/file3.txt"] = new MockFileData("content")
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.DeleteAsync("/test", recursive: true, ct: cts.Token));
    }

    [Fact]
    public async Task AllOperations_ShouldRaiseOperationFailedOnCancellation()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/test/file.txt"] = new MockFileData("content")
        });
        var service = new FileOperationService(mockFileSystem);
        OperationFailedEventArgs? failedEvent = null;
        service.OperationFailed += (s, e) => failedEvent = e;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        try
        {
            await service.DeleteAsync("/test/file.txt", ct: cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - Operation failed event should be raised
        Assert.NotNull(failedEvent);
        Assert.Equal("Delete", failedEvent.OperationName);
        Assert.IsAssignableFrom<OperationCanceledException>(failedEvent.Error);
    }

    [Fact]
    public async Task CopyAsync_WithTimeout_ShouldCancelAfterDelay()
    {
        // Arrange
        var files = new Dictionary<string, MockFileData>();
        for (int i = 0; i < 50; i++)
        {
            files[$"/source/file{i}.txt"] = new MockFileData(new byte[1024]);
        }
        var mockFileSystem = new MockFileSystem(files);
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(1)); // Very short timeout

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await service.CopyAsync("/source", "/dest", ct: cts.Token);
        });

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task MoveAsync_FallbackOperation_ShouldRespectCancellation()
    {
        // Arrange - Create scenario where fallback (copy+delete) is needed
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/source/file1.txt"] = new MockFileData(new byte[1024]),
            ["/source/file2.txt"] = new MockFileData(new byte[1024])
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.MoveAsync("/source", "/dest", ct: cts.Token));
    }

    [Fact]
    public async Task CopyAsync_CancellationBetweenFiles_ShouldNotCorruptDestination()
    {
        // Arrange
        var files = new Dictionary<string, MockFileData>();
        for (int i = 0; i < 10; i++)
        {
            files[$"/source/file{i}.txt"] = new MockFileData($"Content {i}");
        }
        var mockFileSystem = new MockFileSystem(files);
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();

        var filesProcessed = 0;
        var progress = new Progress<OperationProgress>(p =>
        {
            filesProcessed++;
            if (filesProcessed == 3)
            {
                cts.Cancel();
            }
        });

        // Act
        try
        {
            await service.CopyAsync("/source", "/dest", progress: progress, ct: cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - Verify files that were copied are complete and valid
        if (mockFileSystem.Directory.Exists("/dest"))
        {
            var copiedFiles = mockFileSystem.Directory.GetFiles("/dest");
            foreach (var file in copiedFiles)
            {
                var content = mockFileSystem.File.ReadAllText(file);
                Assert.NotEmpty(content); // Each file should have content
                Assert.Contains("Content", content); // Should match expected pattern
            }
        }
    }
}

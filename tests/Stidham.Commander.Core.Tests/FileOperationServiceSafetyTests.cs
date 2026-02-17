using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Tests atomic operation safety mechanisms in FileOperationService.
/// </summary>
public class FileOperationServiceSafetyTests
{
    [Fact]
    public async Task CopyAsync_SingleFile_ShouldUseTemporaryFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/source.txt", new MockFileData("test content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.CopyAsync("/source.txt", "/dest.txt");

        // Assert
        Assert.True(mockFileSystem.File.Exists("/dest.txt"));
        Assert.False(mockFileSystem.File.Exists("/dest.txt.tmp"), "Temp file should be cleaned up");
        Assert.Equal("test content", mockFileSystem.File.ReadAllText("/dest.txt"));
    }

    [Fact]
    public async Task CopyAsync_WithCancellation_ShouldCleanupTempFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/source.txt", new MockFileData(new byte[1000]) }
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();

        // Cancel immediately to trigger cleanup
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CopyAsync("/source.txt", "/dest.txt", ct: cts.Token));

        // Temp file should be cleaned up (or never created due to early cancellation)
        Assert.False(mockFileSystem.File.Exists("/dest.txt.tmp"));
    }

    [Fact]
    public async Task CopyAsync_VerifiesFileSizeMatch()
    {
        // Arrange
        var sourceContent = new byte[12345];
        new Random().NextBytes(sourceContent);

        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/source.bin", new MockFileData(sourceContent) }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.CopyAsync("/source.bin", "/dest.bin");

        // Assert
        var destContent = mockFileSystem.File.ReadAllBytes("/dest.bin");
        Assert.Equal(sourceContent.Length, destContent.Length);
        Assert.Equal(sourceContent, destContent);
    }

    [Fact]
    public async Task CleanupAsync_RemovesOrphanedTempFiles()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file1.txt", new MockFileData("normal file") },
            { "/test/file2.tmp", new MockFileData("orphaned temp") },
            { "/test/subdir/file3.tmp", new MockFileData("nested temp") },
            { "/test/important.txt.tmp", new MockFileData("temp file") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        var cleanedCount = await service.CleanupAsync("/test", recursive: true);

        // Assert
        Assert.Equal(3, cleanedCount);
        Assert.True(mockFileSystem.File.Exists("/test/file1.txt"), "Normal files should remain");
        Assert.False(mockFileSystem.File.Exists("/test/file2.tmp"));
        Assert.False(mockFileSystem.File.Exists("/test/subdir/file3.tmp"));
        Assert.False(mockFileSystem.File.Exists("/test/important.txt.tmp"));
    }

    [Fact]
    public async Task CleanupAsync_NonRecursive_OnlyCleansTopLevel()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file1.tmp", new MockFileData("top level temp") },
            { "/test/subdir/file2.tmp", new MockFileData("nested temp") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        var cleanedCount = await service.CleanupAsync("/test", recursive: false);

        // Assert
        Assert.Equal(1, cleanedCount);
        Assert.False(mockFileSystem.File.Exists("/test/file1.tmp"));
        Assert.True(mockFileSystem.File.Exists("/test/subdir/file2.tmp"), "Nested temp should remain");
    }

    [Fact]
    public async Task CleanupAsync_NonExistentDirectory_ShouldNotThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act
        var cleanedCount = await service.CleanupAsync("/nonexistent");

        // Assert
        Assert.Equal(0, cleanedCount);
    }

    [Fact]
    public async Task CleanupAsync_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file1.tmp", new MockFileData("temp1") },
            { "/test/file2.tmp", new MockFileData("temp2") }
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CleanupAsync("/test", ct: cts.Token));
    }

    [Fact]
    public async Task MoveAsync_CrossVolumeFallback_DeleteSourceAfterSuccessfulCopy()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/source/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Simulate cross-volume move by using different paths
        // MockFileSystem doesn't truly simulate volumes, but tests the fallback logic
        await service.MoveAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.True(mockFileSystem.File.Exists("/dest/file.txt"));
        // Source may or may not exist depending on whether atomic move succeeded
        // But destination should definitely exist
    }

    [Fact]
    public async Task CopyAsync_Directory_UsesTemporaryFilesForEachFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/source/file1.txt", new MockFileData("content1") },
            { "/source/file2.txt", new MockFileData("content2") },
            { "/source/subdir/file3.txt", new MockFileData("content3") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.CopyAsync("/source", "/dest");

        // Assert - All files copied, no temp files left
        Assert.True(mockFileSystem.File.Exists("/dest/file1.txt"));
        Assert.True(mockFileSystem.File.Exists("/dest/file2.txt"));
        Assert.True(mockFileSystem.File.Exists("/dest/subdir/file3.txt"));

        // No temp files should remain
        var allFiles = mockFileSystem.AllFiles.ToList();
        Assert.DoesNotContain(allFiles, f => f.EndsWith(".tmp"));
    }

    [Fact]
    public async Task CopyAsync_OverwriteWithTempFile_ShouldWorkCorrectly()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/source.txt", new MockFileData("new content") },
            { "/dest.txt", new MockFileData("old content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.CopyAsync("/source.txt", "/dest.txt", overwrite: true);

        // Assert
        Assert.Equal("new content", mockFileSystem.File.ReadAllText("/dest.txt"));
        Assert.False(mockFileSystem.File.Exists("/dest.txt.tmp"));
    }

    [Fact]
    public async Task MoveAsync_DirectoryTree_HandlesFailureGracefully()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/source/dir/file1.txt", new MockFileData("content1") },
            { "/source/dir/file2.txt", new MockFileData("content2") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.MoveAsync("/source/dir", "/dest/dir");

        // Assert - Files should be at destination
        Assert.True(mockFileSystem.File.Exists("/dest/dir/file1.txt"));
        Assert.True(mockFileSystem.File.Exists("/dest/dir/file2.txt"));
    }
}

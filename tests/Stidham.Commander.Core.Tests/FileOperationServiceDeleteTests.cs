using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Exceptions;
using Stidham.Commander.Core.Models;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Tests Step 4: DeleteAsync full implementation with MockFileSystem.
/// </summary>
public class FileOperationServiceDeleteTests
{
    [Fact]
    public async Task DeleteAsync_SingleFile_ShouldDeleteFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.DeleteAsync("/test/file.txt");

        // Assert
        Assert.False(mockFileSystem.File.Exists("/test/file.txt"));
    }

    [Fact]
    public async Task DeleteAsync_EmptyDirectory_NonRecursive_ShouldDeleteDirectory()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/emptydir/", new MockDirectoryData() }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.DeleteAsync("/test/emptydir", recursive: false);

        // Assert
        Assert.False(mockFileSystem.Directory.Exists("/test/emptydir"));
    }

    [Fact]
    public async Task DeleteAsync_DirectoryWithFiles_Recursive_ShouldDeleteTree()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/dir/file1.txt", new MockFileData("content1") },
            { "/test/dir/file2.txt", new MockFileData("content2") },
            { "/test/dir/subdir/file3.txt", new MockFileData("content3") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.DeleteAsync("/test/dir", recursive: true);

        // Assert
        Assert.False(mockFileSystem.Directory.Exists("/test/dir"));
        Assert.False(mockFileSystem.File.Exists("/test/dir/file1.txt"));
        Assert.False(mockFileSystem.File.Exists("/test/dir/file2.txt"));
        Assert.False(mockFileSystem.File.Exists("/test/dir/subdir/file3.txt"));
    }

    [Fact]
    public async Task DeleteAsync_DirectoryWithFiles_NonRecursive_ShouldThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/dir/file1.txt", new MockFileData("content1") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(() =>
            service.DeleteAsync("/test/dir", recursive: false));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentFile_ShouldSucceedIdempotently()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert - Should not throw
        await service.DeleteAsync("/nonexistent/file.txt");
    }

    [Fact]
    public async Task DeleteAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException derives from OperationCanceledException
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.DeleteAsync("/test/file.txt", ct: cts.Token));

        Assert.True(ex is TaskCanceledException or OperationCanceledException);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRaiseStartedAndCompletedEvents()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);
        var eventSequence = new List<string>();

        service.OperationStarted += (sender, args) => eventSequence.Add($"Started:{args.OperationName}");
        service.OperationCompleted += (sender, args) => eventSequence.Add($"Completed:{args.OperationName}");

        // Act
        await service.DeleteAsync("/test/file.txt");

        // Assert
        Assert.Equal(["Started:Delete", "Completed:Delete"], eventSequence);
    }

    [Fact]
    public async Task DeleteAsync_WhenExceptionOccurs_ShouldRaiseFailedEvent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/dir/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);
        var failedEventRaised = false;
        OperationFailedEventArgs? failedArgs = null;

        service.OperationFailed += (sender, args) =>
        {
            failedEventRaised = true;
            failedArgs = args;
        };

        // Act & Assert - Try to delete non-empty directory without recursive flag
        await Assert.ThrowsAsync<IOException>(() =>
            service.DeleteAsync("/test/dir", recursive: false));

        Assert.True(failedEventRaised);
        Assert.NotNull(failedArgs);
        Assert.Equal("Delete", failedArgs.OperationName);
        Assert.Equal("/test/dir", failedArgs.Path);
        Assert.IsType<IOException>(failedArgs.Error);
    }

    [Fact]
    public async Task DeleteAsync_ShouldProvideCorrectPathInStartedEvent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/my/special/path.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);
        OperationStartedEventArgs? startedArgs = null;

        service.OperationStarted += (sender, args) => startedArgs = args;

        // Act
        await service.DeleteAsync("/my/special/path.txt");

        // Assert
        Assert.NotNull(startedArgs);
        Assert.Equal("/my/special/path.txt", startedArgs.Path);
    }

    [Fact]
    public async Task DeleteAsync_ReadOnlyFile_ShouldThrowAndRaiseFailedEvent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/readonly/file.txt", new MockFileData("content") { Attributes = System.IO.FileAttributes.ReadOnly } }
        });
        var service = new FileOperationService(mockFileSystem);
        var failedEventRaised = false;

        service.OperationFailed += (sender, args) => failedEventRaised = true;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InsufficientPermissionsException>(() =>
            service.DeleteAsync("/readonly/file.txt"));

        Assert.True(failedEventRaised);
        Assert.Equal("Delete", ex.OperationName);
        Assert.Equal("/readonly/file.txt", ex.Path);
    }

    [Fact]
    public async Task DeleteAsync_MultipleFiles_ShouldDeleteEachSuccessfully()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file1.txt", new MockFileData("content1") },
            { "/test/file2.txt", new MockFileData("content2") },
            { "/test/file3.txt", new MockFileData("content3") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.DeleteAsync("/test/file1.txt");
        await service.DeleteAsync("/test/file2.txt");
        await service.DeleteAsync("/test/file3.txt");

        // Assert
        Assert.False(mockFileSystem.File.Exists("/test/file1.txt"));
        Assert.False(mockFileSystem.File.Exists("/test/file2.txt"));
        Assert.False(mockFileSystem.File.Exists("/test/file3.txt"));
    }

    [Fact]
    public async Task DeleteAsync_DeeplyNestedDirectory_ShouldDeleteEntireTree()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/root/level1/level2/level3/level4/file.txt", new MockFileData("deep") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.DeleteAsync("/root", recursive: true);

        // Assert
        Assert.False(mockFileSystem.Directory.Exists("/root"));
    }

    [Fact]
    public async Task DeleteAsync_CompletedEvent_ShouldHaveCorrectOperationName()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);
        OperationCompletedEventArgs? completedArgs = null;

        service.OperationCompleted += (sender, args) => completedArgs = args;

        // Act
        await service.DeleteAsync("/test/file.txt");

        // Assert
        Assert.NotNull(completedArgs);
        Assert.Equal("Delete", completedArgs.OperationName);
    }
}

using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Models;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Tests Step 5: RenameAsync full implementation with MockFileSystem.
/// </summary>
public class FileOperationServiceRenameTests
{
    [Fact]
    public async Task RenameAsync_File_ShouldRenameFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/oldname.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.RenameAsync("/test/oldname.txt", "/test/newname.txt");

        // Assert
        Assert.False(mockFileSystem.File.Exists("/test/oldname.txt"));
        Assert.True(mockFileSystem.File.Exists("/test/newname.txt"));
        Assert.Equal("content", mockFileSystem.File.ReadAllText("/test/newname.txt"));
    }

    [Fact]
    public async Task RenameAsync_Directory_ShouldRenameDirectory()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/olddir/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.RenameAsync("/test/olddir", "/test/newdir");

        // Assert
        Assert.False(mockFileSystem.Directory.Exists("/test/olddir"));
        Assert.True(mockFileSystem.Directory.Exists("/test/newdir"));
        Assert.True(mockFileSystem.File.Exists("/test/newdir/file.txt"));
    }

    [Fact]
    public async Task RenameAsync_NonExistentFile_ShouldThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.RenameAsync("/nonexistent/file.txt", "/test/newname.txt"));
    }

    [Fact]
    public async Task RenameAsync_FileToExistingName_ShouldThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file1.txt", new MockFileData("content1") },
            { "/test/file2.txt", new MockFileData("content2") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(() =>
            service.RenameAsync("/test/file1.txt", "/test/file2.txt"));
    }

    [Fact]
    public async Task RenameAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RenameAsync("/test/file.txt", "/test/renamed.txt", ct: cts.Token));

        Assert.True(ex is TaskCanceledException or OperationCanceledException);
    }

    [Fact]
    public async Task RenameAsync_ShouldRaiseStartedAndCompletedEvents()
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
        await service.RenameAsync("/test/file.txt", "/test/renamed.txt");

        // Assert
        Assert.Equal(["Started:Rename", "Completed:Rename"], eventSequence);
    }

    [Fact]
    public async Task RenameAsync_WhenExceptionOccurs_ShouldRaiseFailedEvent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);
        var failedEventRaised = false;
        OperationFailedEventArgs? failedArgs = null;

        service.OperationFailed += (sender, args) =>
        {
            failedEventRaised = true;
            failedArgs = args;
        };

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.RenameAsync("/nonexistent/file.txt", "/test/renamed.txt"));

        Assert.True(failedEventRaised);
        Assert.NotNull(failedArgs);
        Assert.Equal("Rename", failedArgs.OperationName);
        Assert.Equal("/nonexistent/file.txt", failedArgs.Path);
        Assert.IsType<FileNotFoundException>(failedArgs.Error);
    }

    [Fact]
    public async Task RenameAsync_ShouldProvideCorrectPathInStartedEvent()
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
        await service.RenameAsync("/my/special/path.txt", "/my/special/renamed.txt");

        // Assert
        Assert.NotNull(startedArgs);
        Assert.Equal("/my/special/path.txt", startedArgs.Path);
    }

    [Fact]
    public async Task RenameAsync_DirectoryWithContents_ShouldRenameEntireTree()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/olddir/file1.txt", new MockFileData("content1") },
            { "/test/olddir/subdir/file2.txt", new MockFileData("content2") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.RenameAsync("/test/olddir", "/test/newdir");

        // Assert
        Assert.False(mockFileSystem.Directory.Exists("/test/olddir"));
        Assert.True(mockFileSystem.Directory.Exists("/test/newdir"));
        Assert.True(mockFileSystem.File.Exists("/test/newdir/file1.txt"));
        Assert.True(mockFileSystem.File.Exists("/test/newdir/subdir/file2.txt"));
    }

    [Fact]
    public async Task RenameAsync_FileWithDifferentExtension_ShouldRename()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/document.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act
        await service.RenameAsync("/test/document.txt", "/test/document.md");

        // Assert
        Assert.False(mockFileSystem.File.Exists("/test/document.txt"));
        Assert.True(mockFileSystem.File.Exists("/test/document.md"));
    }

    [Fact]
    public async Task RenameAsync_CompletedEvent_ShouldHaveCorrectOperationName()
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
        await service.RenameAsync("/test/file.txt", "/test/renamed.txt");

        // Assert
        Assert.NotNull(completedArgs);
        Assert.Equal("Rename", completedArgs.OperationName);
    }

    [Fact]
    public async Task RenameAsync_MultipleSequentialRenames_ShouldSucceed()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act - Chain of renames
        await service.RenameAsync("/test/file.txt", "/test/file1.txt");
        await service.RenameAsync("/test/file1.txt", "/test/file2.txt");
        await service.RenameAsync("/test/file2.txt", "/test/final.txt");

        // Assert
        Assert.False(mockFileSystem.File.Exists("/test/file.txt"));
        Assert.False(mockFileSystem.File.Exists("/test/file1.txt"));
        Assert.False(mockFileSystem.File.Exists("/test/file2.txt"));
        Assert.True(mockFileSystem.File.Exists("/test/final.txt"));
    }
}

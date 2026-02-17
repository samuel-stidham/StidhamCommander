using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Models;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Tests Step 3: FileOperationService observable plumbing (events, IProgress, CancellationToken signatures).
/// Full operation implementations will be tested in Steps 4-7.
/// </summary>
public class FileOperationServiceObservabilityTests
{
    [Fact]
    public async Task DeleteAsync_ShouldRaiseOperationStartedEvent()
    {
        // Arrange
        var service = new FileOperationService();
        var eventRaised = false;
        OperationStartedEventArgs? eventArgs = null;

        service.OperationStarted += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        await service.DeleteAsync("/test/file.txt");

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal("Delete", eventArgs.OperationName);
        Assert.Equal("/test/file.txt", eventArgs.Path);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRaiseOperationCompletedEvent()
    {
        // Arrange
        var service = new FileOperationService();
        var eventRaised = false;
        OperationCompletedEventArgs? eventArgs = null;

        service.OperationCompleted += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        await service.DeleteAsync("/test/file.txt");

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal("Delete", eventArgs.OperationName);
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
        var eventRaised = false;
        OperationStartedEventArgs? eventArgs = null;

        service.OperationStarted += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        await service.CopyAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal("Copy", eventArgs.OperationName);
        Assert.Equal("/source/file.txt", eventArgs.Path);
    }

    [Fact]
    public async Task RenameAsync_ShouldRaiseOperationStartedEvent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);
        var eventRaised = false;
        OperationStartedEventArgs? eventArgs = null;

        service.OperationStarted += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        await service.RenameAsync("/test/file.txt", "/test/renamed.txt");

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal("Rename", eventArgs.OperationName);
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
        var eventRaised = false;
        OperationStartedEventArgs? eventArgs = null;

        service.OperationStarted += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        await service.MoveAsync("/source/file.txt", "/dest/file.txt");

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal("Move", eventArgs.OperationName);
    }

    [Fact]
    public async Task AllOperations_ShouldAcceptIProgressParameter()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file.txt", new MockFileData("content") },
            { "/src/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);
        var progress = new Progress<OperationProgress>();

        // Act & Assert - Should not throw
        await service.DeleteAsync("/test/file.txt", progress: progress);
        await service.CopyAsync("/src/file.txt", "/dst/file.txt", progress: progress);
        await service.RenameAsync("/src/file.txt", "/src/renamed.txt", progress: progress);
        await service.MoveAsync("/src/renamed.txt", "/dst/moved.txt", progress: progress);
    }

    [Fact]
    public async Task AllOperations_ShouldAcceptCancellationToken()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/test/file.txt", new MockFileData("content") },
            { "/src/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);
        var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw with valid token
        await service.DeleteAsync("/test/file.txt", ct: cts.Token);
        await service.CopyAsync("/src/file.txt", "/dst/file.txt", ct: cts.Token);
        await service.RenameAsync("/src/file.txt", "/src/renamed.txt", ct: cts.Token);
        await service.MoveAsync("/src/renamed.txt", "/dst/moved.txt", ct: cts.Token);
    }

    [Fact]
    public async Task EventSequence_ShouldBeStartedThenCompleted()
    {
        // Arrange
        var service = new FileOperationService();
        var eventSequence = new List<string>();

        service.OperationStarted += (sender, args) => eventSequence.Add("Started");
        service.OperationCompleted += (sender, args) => eventSequence.Add("Completed");
        service.OperationFailed += (sender, args) => eventSequence.Add("Failed");

        // Act
        await service.DeleteAsync("/test/file.txt");

        // Assert
        Assert.Equal(["Started", "Completed"], eventSequence);
    }
}


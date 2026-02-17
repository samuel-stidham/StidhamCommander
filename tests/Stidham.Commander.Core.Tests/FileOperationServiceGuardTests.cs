using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Models;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Tests GuardProtectedPath functionality for cross-platform path protection.
/// </summary>
public class FileOperationServiceGuardTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/bin")]
    [InlineData("/sbin")]
    [InlineData("/etc")]
    [InlineData("/usr")]
    [InlineData("/sys")]
    [InlineData("/proc")]
    [InlineData("/boot")]
    [InlineData("/dev")]
    public async Task DeleteAsync_ProtectedSystemPath_ShouldThrowUnauthorizedAccess(string protectedPath)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync(protectedPath));

        Assert.Contains("protected path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/bin")]
    [InlineData("/etc")]
    public async Task RenameAsync_ProtectedSystemPath_ShouldThrowUnauthorizedAccess(string protectedPath)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RenameAsync(protectedPath, "/newname"));

        Assert.Contains("protected path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/usr")]
    [InlineData("/sys")]
    public async Task CopyAsync_ProtectedSystemPath_ShouldThrowUnauthorizedAccess(string protectedPath)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CopyAsync(protectedPath, "/backup"));

        Assert.Contains("protected path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/bin")]
    [InlineData("/etc")]
    public async Task MoveAsync_ProtectedSystemPath_ShouldThrowUnauthorizedAccess(string protectedPath)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.MoveAsync(protectedPath, "/newlocation"));

        Assert.Contains("protected path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_UserHomeRoot_ShouldThrowUnauthorizedAccess()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act & Assert - User home root should be protected
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync(userHome));

        Assert.Contains("protected path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_UserHomeSubdirectory_ShouldSucceed()
    {
        // Arrange
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var subPath = Path.Combine(userHome, "Documents", "test.txt");

        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { subPath, new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert - Subdirectories should NOT be protected
        await service.DeleteAsync(subPath);

        Assert.False(mockFileSystem.File.Exists(subPath));
    }

    [Fact]
    public async Task DeleteAsync_NonProtectedPath_ShouldSucceed()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/home/user/documents/file.txt", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        await service.DeleteAsync("/home/user/documents/file.txt");

        Assert.False(mockFileSystem.File.Exists("/home/user/documents/file.txt"));
    }

    [Fact]
    public async Task DeleteAsync_PathWithTrailingSlash_ShouldStillBeProtected()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert - Path normalization should handle trailing slashes
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync("/bin/"));

        Assert.Contains("protected path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddProtectedPath_CustomPath_ShouldPreventOperations()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/custom/important/data", new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Add custom protected path
        service.AddProtectedPath("/custom/important");

        // Act & Assert - Custom protected path should throw
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync("/custom/important"));

        Assert.Contains("protected path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveProtectedPath_ThenDelete_ShouldSucceed()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/bin", new MockDirectoryData() }
        });
        var service = new FileOperationService(mockFileSystem);

        // Remove protection (dangerous, but allowed)
        service.RemoveProtectedPath("/bin");

        // Act & Assert - Should now succeed
        await service.DeleteAsync("/bin");

        Assert.False(mockFileSystem.Directory.Exists("/bin"));
    }

    [Fact]
    public async Task GuardProtectedPath_ShouldRaiseFailedEvent()
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
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync("/etc"));

        Assert.True(failedEventRaised);
        Assert.NotNull(failedArgs);
        Assert.Equal("Delete", failedArgs.OperationName);
        Assert.IsType<UnauthorizedAccessException>(failedArgs.Error);
    }

    [Theory]
    [InlineData("/tmp/test.txt")]
    [InlineData("/opt/myapp/data.txt")]
    [InlineData("/var/log/custom/app.log")]
    public async Task DeleteAsync_NonSystemPaths_ShouldSucceed(string safePath)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { safePath, new MockFileData("content") }
        });
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert - These paths should not be protected
        await service.DeleteAsync(safePath);

        Assert.False(mockFileSystem.File.Exists(safePath));
    }
}

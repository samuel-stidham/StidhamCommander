using System.IO.Abstractions.TestingHelpers;
using Stidham.Commander.Core.Services;
using Xunit;

namespace Stidham.Commander.Core.Tests;

/// <summary>
/// Tests input validation for FileOperationService methods.
/// </summary>
public class FileOperationServiceValidationTests
{
    #region DeleteAsync Validation

    [Fact]
    public async Task DeleteAsync_NullPath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.DeleteAsync(null!));

        Assert.Equal("path", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task DeleteAsync_EmptyOrWhitespacePath_ShouldThrowArgumentException(string invalidPath)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.DeleteAsync(invalidPath));

        Assert.Equal("path", ex.ParamName);
        Assert.Contains("empty or whitespace", ex.Message);
    }

    #endregion

    #region RenameAsync Validation

    [Fact]
    public async Task RenameAsync_NullPath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.RenameAsync(null!, "/newname"));

        Assert.Equal("path", ex.ParamName);
    }

    [Fact]
    public async Task RenameAsync_NullNewName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.RenameAsync("/oldname", null!));

        Assert.Equal("newName", ex.ParamName);
    }

    [Fact]
    public async Task RenameAsync_EmptyNewName_ShouldThrowArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RenameAsync("/oldname", ""));

        Assert.Equal("newName", ex.ParamName);
    }

    [Fact]
    public async Task RenameAsync_SameSourceAndDestination_ShouldThrowArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RenameAsync("/home/user/file.txt", "/home/user/file.txt"));

        Assert.Contains("same", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CopyAsync Validation

    [Fact]
    public async Task CopyAsync_NullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.CopyAsync(null!, "/dest"));

        Assert.Equal("source", ex.ParamName);
    }

    [Fact]
    public async Task CopyAsync_NullDestination_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.CopyAsync("/source", null!));

        Assert.Equal("destination", ex.ParamName);
    }

    [Fact]
    public async Task CopyAsync_EmptySource_ShouldThrowArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CopyAsync("", "/dest"));

        Assert.Equal("source", ex.ParamName);
    }

    [Fact]
    public async Task CopyAsync_EmptyDestination_ShouldThrowArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CopyAsync("/source", "   "));

        Assert.Equal("destination", ex.ParamName);
    }

    [Fact]
    public async Task CopyAsync_SameSourceAndDestination_ShouldThrowArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CopyAsync("/home/user/file.txt", "/home/user/file.txt"));

        Assert.Contains("same", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/source/file.txt", "/source/FILE.TXT")]  // Case-insensitive on most systems
    [InlineData("/source/dir/", "/source/dir")]  // Trailing slash normalization
    public async Task CopyAsync_EquivalentPaths_ShouldThrowArgumentException(string source, string destination)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CopyAsync(source, destination));

        Assert.Contains("same", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region MoveAsync Validation

    [Fact]
    public async Task MoveAsync_NullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.MoveAsync(null!, "/dest"));

        Assert.Equal("source", ex.ParamName);
    }

    [Fact]
    public async Task MoveAsync_NullDestination_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.MoveAsync("/source", null!));

        Assert.Equal("destination", ex.ParamName);
    }

    [Fact]
    public async Task MoveAsync_EmptySource_ShouldThrowArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.MoveAsync("", "/dest"));

        Assert.Equal("source", ex.ParamName);
    }

    [Fact]
    public async Task MoveAsync_EmptyDestination_ShouldThrowArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.MoveAsync("/source", ""));

        Assert.Equal("destination", ex.ParamName);
    }

    [Fact]
    public async Task MoveAsync_SameSourceAndDestination_ShouldThrowArgumentException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new FileOperationService(mockFileSystem);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.MoveAsync("/home/user/file.txt", "/home/user/file.txt"));

        Assert.Contains("same", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

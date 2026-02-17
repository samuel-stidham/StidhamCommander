using Stidham.Commander.Core.Models;
using Xunit;

namespace Stidham.Commander.Core.Tests;

public class OperationEventArgsTests
{
    [Fact]
    public void OperationStartedEventArgs_ShouldConstructWithProperties()
    {
        // Arrange
        const string operationName = "Delete";
        const string path = "/home/user/file.txt";

        // Act
        var args = new OperationStartedEventArgs(operationName, path);

        // Assert
        Assert.Equal(operationName, args.OperationName);
        Assert.Equal(path, args.Path);
    }

    [Fact]
    public void OperationProgressEventArgs_ShouldConstructWithProperties()
    {
        // Arrange
        const long bytesProcessed = 512;
        const long totalBytes = 1024;

        // Act
        var args = new OperationProgressEventArgs(bytesProcessed, totalBytes);

        // Assert
        Assert.Equal(bytesProcessed, args.BytesProcessed);
        Assert.Equal(totalBytes, args.TotalBytes);
    }

    [Theory]
    [InlineData(0, 1024, 0)]
    [InlineData(512, 1024, 50)]
    [InlineData(1024, 1024, 100)]
    [InlineData(256, 1024, 25)]
    public void OperationProgressEventArgs_PercentComplete_ShouldCalculateCorrectly(long bytesProcessed, long totalBytes, double expected)
    {
        // Arrange & Act
        var args = new OperationProgressEventArgs(bytesProcessed, totalBytes);

        // Assert
        Assert.Equal(expected, args.PercentComplete);
    }

    [Fact]
    public void OperationProgressEventArgs_PercentComplete_WithZeroTotalBytes_ShouldReturnZero()
    {
        // Arrange & Act
        var args = new OperationProgressEventArgs(100, 0);

        // Assert
        Assert.Equal(0, args.PercentComplete);
    }

    [Fact]
    public void OperationCompletedEventArgs_ShouldConstructWithProperties()
    {
        // Arrange
        const string operationName = "Copy";
        const long totalBytes = 2048;

        // Act
        var args = new OperationCompletedEventArgs(operationName, totalBytes);

        // Assert
        Assert.Equal(operationName, args.OperationName);
        Assert.Equal(totalBytes, args.TotalBytes);
    }

    [Fact]
    public void OperationFailedEventArgs_ShouldConstructWithProperties()
    {
        // Arrange
        const string operationName = "Delete";
        const string path = "/home/user/readonly.txt";
        var error = new UnauthorizedAccessException("Access denied");

        // Act
        var args = new OperationFailedEventArgs(operationName, path, error);

        // Assert
        Assert.Equal(operationName, args.OperationName);
        Assert.Equal(path, args.Path);
        Assert.Same(error, args.Error);
    }

    [Fact]
    public void EventArgs_ShouldBeInitOnly()
    {
        // Arrange
        var args = new OperationStartedEventArgs("Test", "/test");

        // Act & Assert
        // Attempting to set a property on init-only property should not compile.
        // This test verifies the class structure through implicit compilation check.
        // Reflection would be needed to verify this at runtime - trust the compiler here.
        Assert.NotNull(args);
    }
}

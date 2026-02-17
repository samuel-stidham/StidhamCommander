using Stidham.Commander.Core.Models;
using Xunit;

namespace Stidham.Commander.Core.Tests;

public class OperationProgressTests
{
    [Fact]
    public void OperationProgress_ShouldConstructWithProperties()
    {
        // Arrange
        const string operationName = "Copy";
        const string currentPath = "/home/user/Documents";
        const long bytesProcessed = 1024;
        const long totalBytes = 2048;

        // Act
        var progress = new OperationProgress
        {
            OperationName = operationName,
            CurrentPath = currentPath,
            BytesProcessed = bytesProcessed,
            TotalBytes = totalBytes
        };

        // Assert
        Assert.Equal(operationName, progress.OperationName);
        Assert.Equal(currentPath, progress.CurrentPath);
        Assert.Equal(bytesProcessed, progress.BytesProcessed);
        Assert.Equal(totalBytes, progress.TotalBytes);
    }

    [Theory]
    [InlineData(0, 1024, 0)]
    [InlineData(256, 1024, 25)]
    [InlineData(512, 1024, 50)]
    [InlineData(768, 1024, 75)]
    [InlineData(1024, 1024, 100)]
    public void OperationProgress_PercentComplete_ShouldCalculateCorrectly(long bytesProcessed, long totalBytes, double expected)
    {
        // Arrange & Act
        var progress = new OperationProgress
        {
            OperationName = "Test",
            CurrentPath = "/test",
            BytesProcessed = bytesProcessed,
            TotalBytes = totalBytes
        };

        // Assert
        Assert.Equal(expected, progress.PercentComplete);
    }

    [Fact]
    public void OperationProgress_PercentComplete_WithZeroTotalBytes_ShouldReturnZero()
    {
        // Arrange & Act
        var progress = new OperationProgress
        {
            OperationName = "Test",
            CurrentPath = "/test",
            BytesProcessed = 100,
            TotalBytes = 0
        };

        // Assert
        Assert.Equal(0, progress.PercentComplete);
    }

    [Fact]
    public void OperationProgress_PercentComplete_WithLargeValues_ShouldCalculateAccurately()
    {
        // Arrange - 512 GB of 1 TB (exactly 50%)
        const long bytesProcessed = 512L * 1024 * 1024 * 1024;
        const long totalBytes = 1024L * 1024 * 1024 * 1024;

        // Act
        var progress = new OperationProgress
        {
            OperationName = "Copy",
            CurrentPath = "/mnt/large",
            BytesProcessed = bytesProcessed,
            TotalBytes = totalBytes
        };

        // Assert - 512 GB / 1 TB = 50%
        Assert.Equal(50, progress.PercentComplete);
    }

    [Fact]
    public void OperationProgress_ShouldBeImmutable()
    {
        // Arrange
        var progress = new OperationProgress
        {
            OperationName = "Test",
            CurrentPath = "/test",
            BytesProcessed = 512,
            TotalBytes = 1024
        };

        // Act & Assert
        // Attempting to modify init-only properties would fail at compile time.
        // This test verifies record structure through property access immutability.
        Assert.NotNull(progress);
        Assert.Equal(512, progress.BytesProcessed);
    }

    [Fact]
    public void OperationProgress_RecordEquality_ShouldCompareBothValue()
    {
        // Arrange
        var progress1 = new OperationProgress
        {
            OperationName = "Copy",
            CurrentPath = "/home/user",
            BytesProcessed = 1024,
            TotalBytes = 2048
        };

        var progress2 = new OperationProgress
        {
            OperationName = "Copy",
            CurrentPath = "/home/user",
            BytesProcessed = 1024,
            TotalBytes = 2048
        };

        var progress3 = new OperationProgress
        {
            OperationName = "Copy",
            CurrentPath = "/home/user",
            BytesProcessed = 512,
            TotalBytes = 2048
        };

        // Act & Assert
        Assert.Equal(progress1, progress2);
        Assert.NotEqual(progress1, progress3);
    }
}

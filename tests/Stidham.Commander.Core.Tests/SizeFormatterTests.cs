using Stidham.Commander.Core.Extensions;
using Xunit;

namespace Stidham.Commander.Core.Tests;

public class SizeFormatterTests
{
    [Theory]
    [InlineData(500, "500.0 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    public void ToHumanReadable_ShouldReturnCorrectFormat(long bytes, string expected)
    {
        // Act
        var result = bytes.ToHumanReadable();

        // Assert
        Assert.Equal(expected, result);
    }
}

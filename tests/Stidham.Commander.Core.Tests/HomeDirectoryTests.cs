using Xunit;
using Stidham.Commander.Core.Services;
using Xunit.Abstractions;

namespace Stidham.Commander.Core.Tests;

public class HomeDirectoryTests(ITestOutputHelper output)
{
    [Fact]
    public void ListHomeDirectory_ManualCheck()
    {
        // Arrange
        var service = new FileDiscoveryService();
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var items = service.GetItems(homePath);

        // Assert
        output.WriteLine($"Listing contents of: {homePath}");
        output.WriteLine(new string('-', 30));

        // C# 14 pattern: Take(10) remains, but we use pattern matching for the label
        foreach (var item in items.Take(10))
        {
            // Property pattern matching 'is' check for clean labeling
            var label = item is { IsDirectory: true } ? "[DIR]" : "[FIL]";
            output.WriteLine($"{label} {item.Name} ({item.FormattedSize})");
        }

        Assert.NotEmpty(items);
    }
}

using Stidham.Commander.Core.Extensions;

namespace Stidham.Commander.Core.Models;

/// <summary>
/// Represents a file system item (file or directory) with metadata.
/// </summary>
public record FileSystemItem
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public long Size { get; init; }
    public bool IsDirectory { get; init; }
    public DateTime LastModified { get; init; }
    public string FormattedSize => IsDirectory ? "<DIR>" : Size.ToHumanReadable();
}

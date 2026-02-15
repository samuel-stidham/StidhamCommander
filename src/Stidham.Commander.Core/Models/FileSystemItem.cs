using Stidham.Commander.Core.Extensions;

namespace Stidham.Commander.Core.Models;

public record FileSystemItem
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public long Size { get; init; }
    public bool IsDirectory { get; init; }
    public DateTime LastModified { get; init; }
    public string FormattedSize => IsDirectory ? "<DIR>" : Size.ToHumanReadable();
}

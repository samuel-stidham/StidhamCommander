using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Stidham.Commander.Core.Exceptions;

namespace Stidham.Commander.Core.Services;

/// <summary>
/// Resolves and normalizes paths for cross-platform stability.
/// </summary>
public class PathResolver(IFileSystem? fileSystem = null, Func<string, string?>? linkTargetResolver = null)
{
    private const int MaxSymlinkDepth = 64;
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
    private readonly Func<string, string?> _linkTargetResolver = linkTargetResolver ?? (_ => null);

    /// <summary>
    /// Resolves a path by expanding tilde, normalizing segments, and resolving symlink chains.
    /// </summary>
    public string ResolvePath(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path), "Path cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));
        }

        var expanded = ExpandTilde(path);
        var normalized = _fileSystem.Path.GetFullPath(expanded);
        return ResolveSymlinks(normalized);
    }

    private string ExpandTilde(string path)
    {
        if (!path.StartsWith("~", StringComparison.Ordinal))
        {
            return path;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
        {
            return path;
        }

        if (path.Length == 1)
        {
            return home;
        }

        var separator = path[1];
        if (separator is '/' or '\\')
        {
            if (path.Length == 2)
            {
                return home;
            }

            var suffix = path[2..];
            return _fileSystem.Path.Combine(home, suffix);
        }

        return path;
    }

    private string ResolveSymlinks(string path)
    {
        var visited = new HashSet<string>(GetPathComparer());
        var current = path;

        for (var depth = 0; depth < MaxSymlinkDepth; depth++)
        {
            if (!visited.Add(current))
            {
                throw new CircularSymlinkException("ResolvePath", current);
            }

            var linkTarget = _linkTargetResolver(current) ?? TryGetLinkTarget(current);
            if (string.IsNullOrEmpty(linkTarget))
            {
                return current;
            }

            var resolvedTarget = _fileSystem.Path.IsPathRooted(linkTarget)
                ? linkTarget
                : _fileSystem.Path.Combine(_fileSystem.Path.GetDirectoryName(current) ?? string.Empty, linkTarget);

            current = _fileSystem.Path.GetFullPath(resolvedTarget);
        }

        throw new CircularSymlinkException("ResolvePath", current);
    }

    private StringComparer GetPathComparer()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    private string? TryGetLinkTarget(string path)
    {
        if (!_fileSystem.File.Exists(path) && !_fileSystem.Directory.Exists(path))
        {
            return null;
        }

        object info = _fileSystem.File.Exists(path)
            ? _fileSystem.FileInfo.New(path)
            : _fileSystem.DirectoryInfo.New(path);

        var linkTargetProperty = info.GetType().GetProperty("LinkTarget");
        if (linkTargetProperty is null)
        {
            return null;
        }

        return linkTargetProperty.GetValue(info) as string;
    }
}

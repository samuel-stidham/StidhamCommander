using System.Collections.Generic;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Stidham.Commander.Core.Models;

namespace Stidham.Commander.Core.Services;

/// <summary>
/// Provides glob-based search over the file system with async streaming.
/// </summary>
public class SearchService(IFileSystem? fileSystem = null)
{
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

    /// <summary>
    /// Searches for files and directories using a glob pattern, returning results as an async stream.
    /// </summary>
    public async IAsyncEnumerable<FileSystemItem> SearchAsync(
        string rootPath,
        string pattern,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (rootPath is null)
        {
            throw new ArgumentNullException(nameof(rootPath), "Root path cannot be null.");
        }

        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern), "Pattern cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path cannot be empty or whitespace.", nameof(rootPath));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern cannot be empty or whitespace.", nameof(pattern));
        }

        ct.ThrowIfCancellationRequested();

        if (!_fileSystem.Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Path not found: {rootPath}");
        }

        var normalizedPattern = NormalizePattern(pattern);
        var matcher = CreateGlobRegex(normalizedPattern);

        foreach (var entry in _fileSystem.Directory.EnumerateFileSystemEntries(rootPath, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = _fileSystem.Path.GetRelativePath(rootPath, entry);
            var normalizedRelative = NormalizeRelativePath(relativePath);

            if (matcher.IsMatch(normalizedRelative))
            {
                yield return CreateFileSystemItem(entry);
                await Task.Yield();
            }
        }
    }

    private static string NormalizePattern(string pattern)
    {
        var normalized = pattern.Replace('\\', '/');
        return normalized.StartsWith("./", StringComparison.Ordinal) ? normalized[2..] : normalized;
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static Regex CreateGlobRegex(string pattern)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < pattern.Length; i++)
        {
            var current = pattern[i];

            if (current == '*')
            {
                var isDoubleStar = i + 1 < pattern.Length && pattern[i + 1] == '*';
                if (isDoubleStar)
                {
                    var hasSlash = i + 2 < pattern.Length && pattern[i + 2] == '/';
                    if (hasSlash)
                    {
                        builder.Append("(?:.*/)?");
                        i += 2;
                        continue;
                    }

                    builder.Append(".*");
                    i++;
                    continue;
                }

                builder.Append("[^/]*");
                continue;
            }

            if (current == '?')
            {
                builder.Append("[^/]");
                continue;
            }

            if (current == '/')
            {
                builder.Append('/');
                continue;
            }

            if ("\\.^$|()[]{}+".IndexOf(current) >= 0)
            {
                builder.Append('\\');
            }

            builder.Append(current);
        }

        var options = RegexOptions.Compiled;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            options |= RegexOptions.IgnoreCase;
        }

        return new Regex($"^{builder}$", options);
    }

    private FileSystemItem CreateFileSystemItem(string path)
    {
        if (_fileSystem.Directory.Exists(path))
        {
            var directoryInfo = _fileSystem.DirectoryInfo.New(path);
            return new FileSystemItem
            {
                Name = directoryInfo.Name,
                FullPath = directoryInfo.FullName,
                IsDirectory = true,
                Size = 0,
                LastModified = directoryInfo.LastWriteTime
            };
        }

        var fileInfo = _fileSystem.FileInfo.New(path);
        return new FileSystemItem
        {
            Name = fileInfo.Name,
            FullPath = fileInfo.FullName,
            IsDirectory = false,
            Size = fileInfo.Length,
            LastModified = fileInfo.LastWriteTime
        };
    }
}

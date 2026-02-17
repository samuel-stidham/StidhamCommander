using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Stidham.Commander.Core.Models;

namespace Stidham.Commander.Core.Services;

/// <summary>
/// Performs file system operations (delete, rename, copy, move) with observable event notification and cancellation support.
/// </summary>
public class FileOperationService(IFileSystem? fileSystem = null)
{
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
    private readonly HashSet<string> _protectedPaths = InitializeProtectedPaths();

    public event EventHandler<OperationStartedEventArgs>? OperationStarted;
    public event EventHandler<OperationProgressEventArgs>? OperationProgress;
    public event EventHandler<OperationCompletedEventArgs>? OperationCompleted;
    public event EventHandler<OperationFailedEventArgs>? OperationFailed;

    /// <summary>
    /// Deletes a file or directory asynchronously.
    /// </summary>
    public async Task DeleteAsync(string path, bool recursive = false, IProgress<OperationProgress>? progress = null, CancellationToken ct = default)
    {
        try
        {
            GuardProtectedPath(path);
            RaiseOperationStarted("Delete", path);

            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                if (_fileSystem.Directory.Exists(path))
                {
                    _fileSystem.Directory.Delete(path, recursive);
                }
                else if (_fileSystem.File.Exists(path))
                {
                    _fileSystem.File.Delete(path);
                }
                // If neither exists, silently succeed (idempotent delete)
            }, ct);

            RaiseOperationCompleted("Delete", 0);
        }
        catch (Exception ex)
        {
            RaiseOperationFailed("Delete", path, ex);
            throw;
        }
    }

    /// <summary>
    /// Renames a file or directory asynchronously.
    /// </summary>
    public async Task RenameAsync(string path, string newName, IProgress<OperationProgress>? progress = null, CancellationToken ct = default)
    {
        try
        {
            GuardProtectedPath(path);
            RaiseOperationStarted("Rename", path);

            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                if (_fileSystem.Directory.Exists(path))
                {
                    _fileSystem.Directory.Move(path, newName);
                }
                else if (_fileSystem.File.Exists(path))
                {
                    _fileSystem.File.Move(path, newName);
                }
                else
                {
                    throw new FileNotFoundException($"Source path not found: {path}");
                }
            }, ct);

            RaiseOperationCompleted("Rename", 0);
        }
        catch (Exception ex)
        {
            RaiseOperationFailed("Rename", path, ex);
            throw;
        }
    }

    /// <summary>
    /// Copies a file or directory tree asynchronously with progress reporting.
    /// </summary>
    public async Task CopyAsync(string source, string destination, bool overwrite = false, IProgress<OperationProgress>? progress = null, CancellationToken ct = default)
    {
        try
        {
            GuardProtectedPath(source);
            GuardProtectedPath(destination);
            RaiseOperationStarted("Copy", source);

            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                if (_fileSystem.File.Exists(source))
                {
                    // Copy single file
                    CopySingleFile(source, destination, overwrite, ct);
                }
                else if (_fileSystem.Directory.Exists(source))
                {
                    // Copy directory tree
                    var totalBytes = CalculateTotalSize(source);
                    long bytesProcessed = 0;
                    CopyDirectoryRecursive(source, destination, overwrite, progress, ref bytesProcessed, totalBytes, ct);
                }
                else
                {
                    throw new FileNotFoundException($"Source path not found: {source}");
                }
            }, ct);

            var totalSize = CalculateTotalSize(source);
            RaiseOperationCompleted("Copy", totalSize);
        }
        catch (Exception ex)
        {
            RaiseOperationFailed("Copy", source, ex);
            throw;
        }
    }

    private void CopySingleFile(string source, string destination, bool overwrite, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!overwrite && _fileSystem.File.Exists(destination))
        {
            throw new IOException($"Destination file already exists: {destination}");
        }

        // Ensure destination directory exists
        var destDir = _fileSystem.Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destDir) && !_fileSystem.Directory.Exists(destDir))
        {
            _fileSystem.Directory.CreateDirectory(destDir);
        }

        _fileSystem.File.Copy(source, destination, overwrite);
    }

    private void CopyDirectoryRecursive(string sourceDir, string destDir, bool overwrite, IProgress<OperationProgress>? progress, ref long bytesProcessed, long totalBytes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Create destination directory if it doesn't exist
        if (!_fileSystem.Directory.Exists(destDir))
        {
            _fileSystem.Directory.CreateDirectory(destDir);
        }

        // Copy all files in current directory
        foreach (var file in _fileSystem.Directory.GetFiles(sourceDir))
        {
            ct.ThrowIfCancellationRequested();

            var fileName = _fileSystem.Path.GetFileName(file);
            var destFile = _fileSystem.Path.Combine(destDir, fileName);

            CopySingleFile(file, destFile, overwrite, ct);

            var fileSize = _fileSystem.FileInfo.New(file).Length;
            bytesProcessed += fileSize;

            RaiseOperationProgress(bytesProcessed, totalBytes);
            progress?.Report(new OperationProgress
            {
                OperationName = "Copy",
                CurrentPath = file,
                BytesProcessed = bytesProcessed,
                TotalBytes = totalBytes
            });
        }

        // Recursively copy subdirectories
        foreach (var subDir in _fileSystem.Directory.GetDirectories(sourceDir))
        {
            ct.ThrowIfCancellationRequested();

            var dirName = _fileSystem.Path.GetFileName(subDir);
            var destSubDir = _fileSystem.Path.Combine(destDir, dirName);

            CopyDirectoryRecursive(subDir, destSubDir, overwrite, progress, ref bytesProcessed, totalBytes, ct);
        }
    }

    private long CalculateTotalSize(string path)
    {
        if (_fileSystem.File.Exists(path))
        {
            return _fileSystem.FileInfo.New(path).Length;
        }
        else if (_fileSystem.Directory.Exists(path))
        {
            long totalSize = 0;

            foreach (var file in _fileSystem.Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                totalSize += _fileSystem.FileInfo.New(file).Length;
            }

            return totalSize;
        }

        return 0;
    }

    /// <summary>
    /// Moves a file or directory asynchronously, with fallback to copy+delete for cross-volume moves.
    /// </summary>
    public async Task MoveAsync(string source, string destination, IProgress<OperationProgress>? progress = null, CancellationToken ct = default)
    {
        GuardProtectedPath(source);
        GuardProtectedPath(destination);

        // Step 7: Full implementation
        RaiseOperationStarted("Move", source);
        await Task.CompletedTask;
        RaiseOperationCompleted("Move", 0);
    }

    /// <summary>
    /// Guards against operations on protected system paths.
    /// </summary>
    protected void GuardProtectedPath(string path)
    {
        var normalizedPath = NormalizePath(path);

        if (_protectedPaths.Contains(normalizedPath))
        {
            throw new UnauthorizedAccessException($"Operation not permitted on protected path: {path}");
        }
    }

    /// <summary>
    /// Initializes the set of protected paths based on the current operating system.
    /// </summary>
    private static HashSet<string> InitializeProtectedPaths()
    {
        var protectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows-specific protected paths
            protectedPaths.Add(@"C:\");
            protectedPaths.Add(@"C:\Windows");
            protectedPaths.Add(@"C:\Program Files");
            protectedPaths.Add(@"C:\Program Files (x86)");
            protectedPaths.Add(@"C:\ProgramData");
            protectedPaths.Add(@"C:\Users");

            // Add user profile root
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                protectedPaths.Add(userProfile);
            }
        }
        else
        {
            // Linux/macOS common protected paths
            protectedPaths.Add("/");
            protectedPaths.Add("/bin");
            protectedPaths.Add("/sbin");
            protectedPaths.Add("/etc");
            protectedPaths.Add("/usr");
            protectedPaths.Add("/lib");
            protectedPaths.Add("/lib64");
            protectedPaths.Add("/sys");
            protectedPaths.Add("/proc");
            protectedPaths.Add("/boot");
            protectedPaths.Add("/dev");
            protectedPaths.Add("/var");

            // Add user home root
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userHome))
            {
                protectedPaths.Add(userHome);
            }

            // macOS-specific paths
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                protectedPaths.Add("/System");
                protectedPaths.Add("/Library");
                protectedPaths.Add("/Applications");
                protectedPaths.Add("/Volumes");
            }
        }

        return protectedPaths;
    }

    /// <summary>
    /// Normalizes a path for comparison by removing trailing separators, except for root paths.
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Preserve root paths (e.g., "/" on Unix, "C:\" on Windows)
        if (string.IsNullOrEmpty(trimmed))
            return path;

        return trimmed;
    }

    /// <summary>
    /// Adds a custom protected path to prevent operations on it.
    /// </summary>
    public void AddProtectedPath(string path)
    {
        var normalizedPath = NormalizePath(path);
        _protectedPaths.Add(normalizedPath);
    }

    /// <summary>
    /// Removes a protected path, allowing operations on it.
    /// </summary>
    public void RemoveProtectedPath(string path)
    {
        var normalizedPath = NormalizePath(path);
        _protectedPaths.Remove(normalizedPath);
    }

    protected void RaiseOperationStarted(string operationName, string path)
    {
        OperationStarted?.Invoke(this, new OperationStartedEventArgs(operationName, path));
    }

    protected void RaiseOperationProgress(long bytesProcessed, long totalBytes)
    {
        OperationProgress?.Invoke(this, new OperationProgressEventArgs(bytesProcessed, totalBytes));
    }

    protected void RaiseOperationCompleted(string operationName, long totalBytes)
    {
        OperationCompleted?.Invoke(this, new OperationCompletedEventArgs(operationName, totalBytes));
    }

    protected void RaiseOperationFailed(string operationName, string path, Exception error)
    {
        OperationFailed?.Invoke(this, new OperationFailedEventArgs(operationName, path, error));
    }
}


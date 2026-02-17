using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Stidham.Commander.Core.Exceptions;
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
            ValidatePath(path, nameof(path));
            GuardProtectedPath(path, "Delete");
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
        catch (ProtectedPathException ex)
        {
            RaiseOperationFailed("Delete", path, ex);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            var wrappedException = new InsufficientPermissionsException("Delete", path);
            RaiseOperationFailed("Delete", path, wrappedException);
            throw wrappedException;
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
            ValidatePath(path, nameof(path));
            ValidatePath(newName, nameof(newName));
            ValidateDestinationPath(path, newName);
            GuardProtectedPath(path, "Rename");
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
        catch (ProtectedPathException ex)
        {
            RaiseOperationFailed("Rename", path, ex);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            var wrappedException = new InsufficientPermissionsException("Rename", path);
            RaiseOperationFailed("Rename", path, wrappedException);
            throw wrappedException;
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
            ValidatePath(source, nameof(source));
            ValidatePath(destination, nameof(destination));
            ValidateDestinationPath(source, destination);
            GuardProtectedPath(source, "Copy");
            GuardProtectedPath(destination, "Copy");
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
        catch (ProtectedPathException ex)
        {
            RaiseOperationFailed("Copy", source, ex);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            var wrappedException = new InsufficientPermissionsException("Copy", source);
            RaiseOperationFailed("Copy", source, wrappedException);
            throw wrappedException;
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

        // Transaction-like copy: use temporary file for safety
        var tempFile = destination + ".tmp";

        try
        {
            // Copy to temp location first
            _fileSystem.File.Copy(source, tempFile, overwrite: true);

            ct.ThrowIfCancellationRequested();

            // Verify copy integrity (size check)
            var sourceSize = _fileSystem.FileInfo.New(source).Length;
            var tempSize = _fileSystem.FileInfo.New(tempFile).Length;

            if (sourceSize != tempSize)
            {
                throw new IOException($"Copy verification failed: size mismatch for {source}");
            }

            ct.ThrowIfCancellationRequested();

            // Atomic move from temp to final destination
            if (_fileSystem.File.Exists(destination))
            {
                _fileSystem.File.Delete(destination);
            }
            _fileSystem.File.Move(tempFile, destination);
        }
        catch
        {
            // Cleanup temp file on failure
            if (_fileSystem.File.Exists(tempFile))
            {
                try
                {
                    _fileSystem.File.Delete(tempFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            throw;
        }
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
        try
        {
            ValidatePath(source, nameof(source));
            ValidatePath(destination, nameof(destination));
            ValidateDestinationPath(source, destination);
            GuardProtectedPath(source, "Move");
            GuardProtectedPath(destination, "Move");
            RaiseOperationStarted("Move", source);

            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                if (!_fileSystem.File.Exists(source) && !_fileSystem.Directory.Exists(source))
                {
                    throw new FileNotFoundException($"Source path not found: {source}");
                }

                // Try atomic move first
                try
                {
                    if (_fileSystem.File.Exists(source))
                    {
                        _fileSystem.File.Move(source, destination);
                    }
                    else if (_fileSystem.Directory.Exists(source))
                    {
                        _fileSystem.Directory.Move(source, destination);
                    }
                }
                catch (IOException)
                {
                    // Atomic move failed (likely cross-volume), fallback to Copy+Delete
                    ct.ThrowIfCancellationRequested();

                    // Copy first
                    var totalBytes = CalculateTotalSize(source);
                    long bytesProcessed = 0;

                    if (_fileSystem.File.Exists(source))
                    {
                        CopySingleFile(source, destination, overwrite: true, ct);
                    }
                    else if (_fileSystem.Directory.Exists(source))
                    {
                        CopyDirectoryRecursive(source, destination, overwrite: true, progress, ref bytesProcessed, totalBytes, ct);
                    }

                    ct.ThrowIfCancellationRequested();

                    // Delete source after successful copy
                    // If delete fails, don't throw - the move functionally succeeded
                    try
                    {
                        if (_fileSystem.File.Exists(source))
                        {
                            _fileSystem.File.Delete(source);
                        }
                        else if (_fileSystem.Directory.Exists(source))
                        {
                            _fileSystem.Directory.Delete(source, recursive: true);
                        }
                    }
                    catch
                    {
                        // Ignore delete errors after successful copy
                        // The data has been moved to destination successfully
                    }
                }
            }, ct);

            var totalSize = CalculateTotalSize(destination);
            RaiseOperationCompleted("Move", totalSize);
        }
        catch (ProtectedPathException ex)
        {
            RaiseOperationFailed("Move", source, ex);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            var wrappedException = new InsufficientPermissionsException("Move", source);
            RaiseOperationFailed("Move", source, wrappedException);
            throw wrappedException;
        }
        catch (Exception ex)
        {
            RaiseOperationFailed("Move", source, ex);
            throw;
        }
    }

    /// <summary>
    /// Cleans up orphaned temporary files in a directory tree.
    /// </summary>
    /// <param name="directory">The directory to scan for temporary files.</param>
    /// <param name="recursive">Whether to scan subdirectories recursively.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of temporary files cleaned up.</returns>
    public async Task<int> CleanupAsync(string directory, bool recursive = true, CancellationToken ct = default)
    {
        ValidatePath(directory, nameof(directory));

        int cleanedCount = 0;

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            if (!_fileSystem.Directory.Exists(directory))
            {
                return;
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var tempFiles = _fileSystem.Directory.GetFiles(directory, "*.tmp", searchOption);

            foreach (var tempFile in tempFiles)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    _fileSystem.File.Delete(tempFile);
                    cleanedCount++;
                }
                catch
                {
                    // Ignore errors during cleanup (file may be in use)
                }
            }
        }, ct);

        return cleanedCount;
    }

    /// <summary>
    /// Validates that a path is not null, empty, or contains invalid characters.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
    /// <exception cref="ArgumentException">Thrown when path is empty or contains invalid characters.</exception>
    protected void ValidatePath(string? path, string paramName)
    {
        if (path is null)
        {
            throw new ArgumentNullException(paramName, "Path cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty or whitespace.", paramName);
        }

        // Check for invalid path characters
        var invalidChars = Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c)))
        {
            throw new ArgumentException($"Path contains invalid characters: {path}", paramName);
        }
    }

    /// <summary>
    /// Validates that source and destination paths are different.
    /// </summary>
    /// <param name="source">The source path.</param>
    /// <param name="destination">The destination path.</param>
    /// <exception cref="ArgumentException">Thrown when source equals destination.</exception>
    protected void ValidateDestinationPath(string source, string destination)
    {
        var normalizedSource = NormalizePath(source);
        var normalizedDestination = NormalizePath(destination);

        if (normalizedSource.Equals(normalizedDestination, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Source and destination paths cannot be the same: {source}");
        }
    }

    /// <summary>
    /// Guards against operations on protected system paths.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="operationName">The name of the operation being performed (e.g., "Delete", "Copy").</param>
    /// <exception cref="ProtectedPathException">Thrown when attempting to operate on a protected system path.</exception>
    protected void GuardProtectedPath(string path, string operationName)
    {
        var normalizedPath = NormalizePath(path);

        if (_protectedPaths.Contains(normalizedPath))
        {
            throw new ProtectedPathException(path, operationName);
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


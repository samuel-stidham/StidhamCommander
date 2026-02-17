using System.IO.Abstractions;
using Stidham.Commander.Core.Models;

namespace Stidham.Commander.Core.Services;

/// <summary>
/// Performs file system operations (delete, rename, copy, move) with observable event notification and cancellation support.
/// </summary>
public class FileOperationService(IFileSystem? fileSystem = null)
{
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

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
        // Step 5: Full implementation
        RaiseOperationStarted("Rename", path);
        await Task.CompletedTask;
        RaiseOperationCompleted("Rename", 0);
    }

    /// <summary>
    /// Copies a file or directory tree asynchronously with progress reporting.
    /// </summary>
    public async Task CopyAsync(string source, string destination, bool overwrite = false, IProgress<OperationProgress>? progress = null, CancellationToken ct = default)
    {
        // Step 6: Full implementation
        RaiseOperationStarted("Copy", source);
        await Task.CompletedTask;
        RaiseOperationCompleted("Copy", 0);
    }

    /// <summary>
    /// Moves a file or directory asynchronously, with fallback to copy+delete for cross-volume moves.
    /// </summary>
    public async Task MoveAsync(string source, string destination, IProgress<OperationProgress>? progress = null, CancellationToken ct = default)
    {
        // Step 7: Full implementation
        RaiseOperationStarted("Move", source);
        await Task.CompletedTask;
        RaiseOperationCompleted("Move", 0);
    }

    /// <summary>
    /// Guards against operations on protected system paths. Placeholder for Phase 2 implementation.
    /// </summary>
    protected void GuardProtectedPath(string path)
    {
        // Phase 2: Implement cross-platform protection logic
        // Protect: /, /bin, /etc, /sys, C:\, C:\Windows, etc.
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


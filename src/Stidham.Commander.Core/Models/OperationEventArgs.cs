namespace Stidham.Commander.Core.Models;

/// <summary>
/// Raised when a file operation begins.
/// </summary>
public sealed class OperationStartedEventArgs(string operationName, string path) : EventArgs
{
    public string OperationName { get; } = operationName;
    public string Path { get; } = path;
}

/// <summary>
/// Raised during a file operation to report progress.
/// </summary>
public sealed class OperationProgressEventArgs(long bytesProcessed, long totalBytes) : EventArgs
{
    public long BytesProcessed { get; } = bytesProcessed;
    public long TotalBytes { get; } = totalBytes;

    /// <summary>
    /// Percentage complete (0-100). Returns 0 if TotalBytes is 0.
    /// </summary>
    public double PercentComplete => TotalBytes > 0 ? (BytesProcessed * 100.0) / TotalBytes : 0;
}

/// <summary>
/// Raised when a file operation completes successfully.
/// </summary>
public sealed class OperationCompletedEventArgs(string operationName, long totalBytes) : EventArgs
{
    public string OperationName { get; } = operationName;
    public long TotalBytes { get; } = totalBytes;
}

/// <summary>
/// Raised when a file operation fails.
/// </summary>
public sealed class OperationFailedEventArgs(string operationName, string path, Exception error) : EventArgs
{
    public string OperationName { get; } = operationName;
    public string Path { get; } = path;
    public Exception Error { get; } = error;
}


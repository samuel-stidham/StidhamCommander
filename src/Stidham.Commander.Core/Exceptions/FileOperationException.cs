namespace Stidham.Commander.Core.Exceptions;

/// <summary>
/// Base exception for file operation errors.
/// </summary>
/// <remarks>
/// Provides common context for all file operations including the operation name,
/// affected path, and optional inner exception for chaining.
/// </remarks>
public class FileOperationException : Exception
{
    public string OperationName { get; }
    public string Path { get; }

    public FileOperationException(string operationName, string path, string message)
        : base(message)
    {
        OperationName = operationName;
        Path = path;
    }

    public FileOperationException(string operationName, string path, string message, Exception innerException)
        : base(message, innerException)
    {
        OperationName = operationName;
        Path = path;
    }
}

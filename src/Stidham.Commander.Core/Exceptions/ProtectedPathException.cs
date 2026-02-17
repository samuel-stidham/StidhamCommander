namespace Stidham.Commander.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to perform a file operation on a protected system path.
/// </summary>
/// <remarks>
/// Protected paths include system directories (e.g., /, /bin, /etc on Unix, C:\Windows on Windows)
/// that require administrator privileges. StidhamCommander does not support privilege elevation
/// and will block all operations on these paths.
/// </remarks>
public class ProtectedPathException(string path, string operationName)
    : UnauthorizedAccessException($"Operation '{operationName}' not permitted on protected path: {path}")
{
    public string Path { get; } = path;
    public string OperationName { get; } = operationName;
}

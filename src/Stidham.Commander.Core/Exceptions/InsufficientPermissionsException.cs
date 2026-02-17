namespace Stidham.Commander.Core.Exceptions;

/// <summary>
/// Exception thrown when a file operation fails due to insufficient permissions.
/// </summary>
/// <remarks>
/// This exception is thrown when the current user lacks the necessary permissions
/// to perform an operation on a file or directory, excluding protected system paths
/// (which throw ProtectedPathException instead). This typically occurs with user-owned
/// files that have restrictive permissions.
/// </remarks>
public class InsufficientPermissionsException(string operationName, string path)
    : FileOperationException(
        operationName,
        path,
        $"Insufficient permissions to perform '{operationName}' on path: {path}")
{
}

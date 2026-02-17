namespace Stidham.Commander.Core.Exceptions;

/// <summary>
/// Exception thrown when a circular symbolic link is detected during path resolution.
/// </summary>
/// <remarks>
/// Circular symlinks create infinite loops during path traversal and must be detected
/// to prevent stack overflows or infinite recursion. This exception provides context
/// about which path and operation encountered the circular reference.
/// </remarks>
public class CircularSymlinkException(string operationName, string path)
    : FileOperationException(
        operationName,
        path,
        $"Circular symbolic link detected at path: {path}")
{
}

namespace Stidham.Commander.Core.Models;

/// <summary>
/// Represents the current progress state of a file operation.
/// </summary>
public record OperationProgress
{
    public required string OperationName { get; init; }
    public required string CurrentPath { get; init; }
    public long BytesProcessed { get; init; }
    public long TotalBytes { get; init; }

    /// <summary>
    /// Percentage complete (0-100). Returns 0 if TotalBytes is 0.
    /// </summary>
    public double PercentComplete => TotalBytes > 0 ? (BytesProcessed * 100.0) / TotalBytes : 0;
}

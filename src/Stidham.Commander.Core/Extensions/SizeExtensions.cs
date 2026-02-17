namespace Stidham.Commander.Core.Extensions;

public static class SizeExtensions
{
    /// <summary>
    /// Converts a byte count to a human-readable size string (B, KB, MB, GB, TB).
    /// </summary>
    extension (long bytes)
    {
        public string ToHumanReadable()
        {
            string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }
    }
}

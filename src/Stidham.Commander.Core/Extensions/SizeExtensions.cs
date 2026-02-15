namespace Stidham.Commander.Core.Extensions;

// C# 14 Extension Member syntax
public static class SizeExtensions
{
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

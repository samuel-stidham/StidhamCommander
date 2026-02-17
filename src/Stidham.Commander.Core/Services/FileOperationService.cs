using System.IO.Abstractions;

namespace Stidham.Commander.Core.Services;

public class FileOperationService(IFileSystem? fileSystem = null)
{
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

    public void Delete(string path, bool recursive = false)
    {
        if (_fileSystem.Directory.Exists(path))
            _fileSystem.Directory.Delete(path, recursive);
        else if (_fileSystem.File.Exists(path))
            _fileSystem.File.Delete(path);
    }

    public void Move(string source, string destination)
    {
        if (_fileSystem.Directory.Exists(source))
            _fileSystem.Directory.Move(source, destination);
        else
            _fileSystem.File.Move(source, destination);
    }

    public async Task CopyAsync(string source, string destination, bool overwrite = false)
    {
        // For files, we use the standard copy.
        // For directories, we'd need a recursive copy helper.
        await Task.Run(() => _fileSystem.File.Copy(source, destination, overwrite));
    }
}

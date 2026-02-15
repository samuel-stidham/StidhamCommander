using System.IO.Abstractions;
using Stidham.Commander.Core.Models;

namespace Stidham.Commander.Core.Services;

public class FileDiscoveryService(IFileSystem? fileSystem = null)
{
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

    public IEnumerable<FileSystemItem> GetItems(string path)
    {
        var directory = _fileSystem.DirectoryInfo.New(path);

        if (!directory.Exists)
            throw new DirectoryNotFoundException($"Path not found: {path}");

        return directory.GetFileSystemInfos()
            .Select(info => new FileSystemItem
            {
                Name = info.Name,
                FullPath = info.FullName,
                IsDirectory = (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory,
                Size = info is IFileInfo f ? f.Length : 0,
                LastModified = info.LastWriteTime
            })
            .OrderByDescending(i => i.IsDirectory)
            .ThenBy(i => i.Name);
    }
}

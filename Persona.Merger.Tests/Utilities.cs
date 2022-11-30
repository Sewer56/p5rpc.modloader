using FileEmulationFramework.Lib.IO;

namespace Persona.Merger.Tests;

public static class Utilities
{
    public static List<FileInformation> GetFilesInDirectory(string folder)
    {
        WindowsDirectorySearcher.GetDirectoryContentsRecursive(folder, out var files, out _);
        return files;
    }
}
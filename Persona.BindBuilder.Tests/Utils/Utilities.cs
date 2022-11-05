using FileEmulationFramework.Lib.IO;

namespace Persona.BindBuilder.Tests.Utils;

public static class Utilities
{
    public static List<FileInformation> GetFilesInDirectory(string folder)
    {
        WindowsDirectorySearcher.GetDirectoryContentsRecursive(folder, out var files, out var directories);
        return files;
    }
}
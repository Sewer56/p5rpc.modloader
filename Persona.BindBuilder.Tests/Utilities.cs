using FileEmulationFramework.Lib.IO;
using Moq;
using Persona.BindBuilder.Interfaces;

namespace Persona.BindBuilder.Tests;

public static class Utilities
{
    public static List<FileInformation> GetFilesInDirectory(string folder)
    {
        WindowsDirectorySearcher.GetDirectoryContentsRecursive(folder, out var files, out var directories);
        return files;
    }

    public static ICurrentProcessProvider GetCurrentProcessProvider(int id)
    {
        var mock = new Mock<ICurrentProcessProvider>();
        mock.Setup(x => x.GetProcessId()).Returns(id);
        return mock.Object;
    }
    
    public static IProcessListProvider GetProcessListProvider(int[] ids)
    {
        var mock = new Mock<IProcessListProvider>();
        mock.Setup(x => x.GetProcessIds()).Returns(ids);
        return mock.Object;
    }
}
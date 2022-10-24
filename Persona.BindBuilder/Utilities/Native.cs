using System.Runtime.InteropServices;

namespace Persona.BindBuilder.Utilities;

public static class Native
{
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode )]
    public static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
}
using System.Runtime.InteropServices;

namespace MonitorDaylightSync;

public static class ConsoleHelper
{
    /// <summary>
    /// Completely detaches application from its console.
    /// </summary>
    /// <returns></returns>
    [DllImport("kernel32.dll")]
    public static extern bool FreeConsole();
}
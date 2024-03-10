using Microsoft.Extensions.Options;
using MonitorDaylightSync.Configuration;

namespace MonitorDaylightSync;

public class CmmCommandExecutor
{
    private readonly MonitorConfiguration _monitorConfiguration;
    private readonly ILogger<CmmCommandExecutor> _logger;

    public CmmCommandExecutor(
        IOptions<MonitorConfiguration> monitorConfiguration, 
        ILogger<CmmCommandExecutor> logger)
    {
        _monitorConfiguration = monitorConfiguration.Value;
        _logger = logger;
    }

    public void Execute(MonitorCommandData data)
    {
        var commands = new List<string>();
        foreach (var monitor in _monitorConfiguration.Monitors)
        {
            commands.Add($"/SetValueIfNeeded " +
                         $"{monitor.Name} " +
                         $"{monitor.Brightness.CmmCode} " +
                         $"{PercentToMonitorValue(monitor.Brightness.Min, monitor.Brightness.Max, data.Brightness)}");
            
            // Contrast is calculated from brightness
            commands.Add($"/SetValueIfNeeded " +
                         $"{monitor.Name} " +
                         $"{monitor.Contrast.CmmCode} " +
                         $"{PercentToMonitorValue(monitor.Contrast.Min, monitor.Contrast.Max, data.Brightness)}");
            
            // TODO: add color
        }
                
        _logger.LogDebug("Executing commands: {@Commands}", commands);
        NativeMethods.LaunchProcess($"ControlMyMonitor {string.Join(" ", commands)}");
    }
    
    private static int PercentToMonitorValue(int min, int max, int percent)
    {
        int range = max - min;
        double multiplied = range * (percent / 100d);
        int rounded = (int)Math.Round(multiplied);
        return min + rounded;
    }
}
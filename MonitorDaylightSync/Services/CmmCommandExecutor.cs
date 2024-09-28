using System.Diagnostics;
using Microsoft.Extensions.Options;
using MonitorDaylightSync.Configuration;
using MonitorDaylightSync.Dtos;

namespace MonitorDaylightSync.Services;

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

    public async Task ExecuteAsync(MonitorCommandDto dto, CancellationToken ct)
    {
        var commands = new List<string>();
        
        foreach (var monitor in _monitorConfiguration.Monitors)
        {
            commands.Add($"/SetValueIfNeeded " +
                         $"{monitor.Name} " +
                         $"{monitor.Brightness.CmmCode} " +
                         $"{PercentToMonitorValue(monitor.Brightness.Min, monitor.Brightness.Max, dto.Brightness)}");
            
            // Contrast is calculated from brightness
            commands.Add($"/SetValueIfNeeded " +
                         $"{monitor.Name} " +
                         $"{monitor.Contrast.CmmCode} " +
                         $"{PercentToMonitorValue(monitor.Contrast.Min, monitor.Contrast.Max, dto.Brightness)}");
            
            // TODO: add color
            // https://tannerhelland.com/2012/09/18/convert-temperature-rgb-algorithm-code.html
        }

        string joinedCommands = string.Join(" ", commands);
        
        _logger.LogDebug("Executing command: ControlMyMonitor {JoinedCommands}", joinedCommands);
        
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ControlMyMonitor",
                Arguments = joinedCommands,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            var stopwatch = new Stopwatch(); // TODO: remove
            stopwatch.Start();
            
            using var process = Process.Start(processStartInfo);
            
            if (process is not null)
                await process.WaitForExitAsync(ct);
            
            stopwatch.Stop();
            _logger.LogDebug("Command executed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Task canceled");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while executing commands");
        }
    }
    
    private static int PercentToMonitorValue(int min, int max, int percent)
    {
        int range = max - min;
        double multiplied = range * (percent / 100d);
        int rounded = (int)Math.Round(multiplied);
        return min + rounded;
    }
}
﻿using System.Diagnostics;
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
        
        test(); // TODO: remove
    }

    void test()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Execute(new MonitorCommandData
                {
                    Brightness = Random.Shared.Next(30, 80)
                });
                
                await Task.Delay(2000);
            }
        });
    }

    public async Task Execute(MonitorCommandData data)
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

        string joinedCommands = string.Join(" ", commands);
        
        _logger.LogInformation("Executing command: ControlMyMonitor {JoinedCommands}", joinedCommands);// TODO: to debug
        
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
            await process.WaitForExitAsync();
            
            stopwatch.Stop();
            _logger.LogInformation("Command executed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
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
using MonitorDaylightSync.Dtos;

namespace MonitorDaylightSync.Services;

public class TestCommandSender : BackgroundService
{
    private int _currentBrightness = 25;
    
    private readonly CmmCommandExecutor _cmmCommandExecutor;
    private readonly ILogger<TestCommandSender> _logger;
    
    public TestCommandSender(
        CmmCommandExecutor cmmCommandExecutor,
        ILogger<TestCommandSender> logger)
    {
        _cmmCommandExecutor = cmmCommandExecutor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!Console.KeyAvailable)
                {
                    await Task.Delay(75, ct);
                    continue;
                }
                
                switch (Console.ReadKey(true).Key)  
                {  
                    case ConsoleKey.UpArrow: 
                        if (_currentBrightness == 100) 
                            continue;
                        
                        _currentBrightness += 10;
                        
                        if (_currentBrightness > 100) 
                            _currentBrightness = 100;
                        
                        break; 
                    
                    case ConsoleKey.DownArrow: 
                        
                        if (_currentBrightness == 0)
                            continue;
                        
                        _currentBrightness -= 10;
                        
                        if (_currentBrightness < 0)
                            _currentBrightness = 0;
                        
                        break;
                    
                    default: continue;
                }  
                
                var cmd = new MonitorCommandDto { Brightness = _currentBrightness };
                await _cmmCommandExecutor.ExecuteAsync(cmd, ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Task canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred in the {nameof(TestCommandSender)} background service");
        }
    }
}
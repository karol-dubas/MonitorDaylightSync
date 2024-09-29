using MonitorDaylightSync.CommandExecutors;
using MonitorDaylightSync.Dtos;

namespace MonitorDaylightSync.BackgroundWorkers;

public class TestCommandSender : BackgroundService
{
    private readonly MonitorCommandDto _cmd = new()
    {
        Brightness = 25,
        Color = 25 
    };
    
    private readonly ICommandExecutor _commandExecutor;
    private readonly ILogger<TestCommandSender> _logger;
    
    public TestCommandSender(
        ICommandExecutor commandExecutor,
        ILogger<TestCommandSender> logger)
    {
        _commandExecutor = commandExecutor;
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
                    case ConsoleKey.UpArrow: _cmd.Brightness += 10; break; 
                    case ConsoleKey.DownArrow: _cmd.Brightness -= 10; break;
                    case ConsoleKey.LeftArrow: _cmd.Color -= 10; break;
                    case ConsoleKey.RightArrow: _cmd.Color += 10; break;
                    default: continue;
                }  

                await _commandExecutor.ExecuteAsync(_cmd, ct);
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
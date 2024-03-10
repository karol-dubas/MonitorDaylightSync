namespace MqttClient;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly MqttClient _mqttClient;

    public Worker(
        ILogger<Worker> logger, 
        MqttClient mqttClient)
    {
        _logger = logger;
        _mqttClient = mqttClient;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("{Name} started", nameof(Worker));
        await _mqttClient.StartLoopAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        await _mqttClient.StopAsync(ct);
        _logger.LogInformation("{Name} stopped", nameof(Worker));
    }
}

using MQTTnet;
using MQTTnet.Client;

namespace MqttClient;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MqttClientConfiguration _mqttConfig;
    private IMqttClient? _mqttClient;

    public Worker(
        ILogger<Worker> logger,
        MqttClientConfiguration mqttConfig)
    {
        _logger = logger;
        _mqttConfig = mqttConfig;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("{Name} started", nameof(Worker));

        var mqttFactory = new MqttFactory();
        _mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttConfig.Address, _mqttConfig.Port)
            .WithCredentials(_mqttConfig.Username, _mqttConfig.Password)
            .Build();

        HandleReceivedMessage();
        await ConnectClient(mqttClientOptions, ct);
        StartReconnectLoop(mqttClientOptions, ct);
        await SubscribeToTopic(mqttFactory, ct);

        await base.StartAsync(ct);
    }

    // TODO: refactor this service
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
                await Task.Delay(1000, ct);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("{Name} cancelled", nameof(Worker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Disconnecting MQTT client...");
            await _mqttClient.DisconnectAsync(cancellationToken: ct);
            _mqttClient!.Dispose();
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("{Name} cancelled", nameof(Worker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disconnecting MQTT client");
        }

        _logger.LogInformation("{Name} stopped", nameof(Worker));
        await base.StopAsync(ct);
    }

    private void StartReconnectLoop(
        MqttClientOptions mqttClientOptions,
        CancellationToken ct)
    {
        try
        {
            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                    try
                    {
                        if (await _mqttClient.TryPingAsync(ct))
                        {
                            _logger.LogDebug("MQTT client is connected");
                        }
                        else
                        {
                            // TODO: is TryPingAsync connecting?
                            // TODO: connect warning -> info

                            _logger.LogWarning("MQTT client not connected, connecting...");
                            var response = await _mqttClient!.ConnectAsync(mqttClientOptions, ct);
                            _logger.LogDebug("Connected: {@Response}", response);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Can't connect MQTT client");
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), ct);
                    }
            }, ct);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("{Name} cancelled", nameof(Worker));
        }
    }
    
    private async Task SubscribeToTopic(MqttFactory mqttFactory, CancellationToken ct)
    {
        try
        {
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(_mqttConfig.Topic))
                .Build();

            var response = await _mqttClient!.SubscribeAsync(mqttSubscribeOptions, ct);
            _logger.LogInformation("Connected to a topic '{Topic}' with response: {@Response}",
                _mqttConfig.Topic, response);
        }
        catch (Exception e)
        {
            // TODO: error handling
        }
    }
    
    private async Task ConnectClient(MqttClientOptions mqttClientOptions, CancellationToken ct)
    {
        var response = await _mqttClient!.ConnectAsync(mqttClientOptions, ct);
        _logger.LogDebug("Connected: {@Response}", response);
    }

    private void HandleReceivedMessage()
    {
        _mqttClient!.ApplicationMessageReceivedAsync += e =>
        {
            // TODO: deserialize json
            // TODO: run all commands
            // TODO: use ControlMyMonitor (path env var?)

            _logger.LogDebug("Received message: {@Message}", e);
            return Task.CompletedTask;
        };
    }
}
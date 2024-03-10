using Microsoft.Extensions.Options;
using MonitorDaylightSync.Configuration;
using MQTTnet;
using MQTTnet.Client;
using SpanJson;

namespace MonitorDaylightSync;

public class MqttClient
{
    private readonly ILogger<MqttClient> _logger;
    private readonly MqttClientConfiguration _mqttConfig;
    private readonly MonitorConfiguration _monitorConfiguration;
    
    private readonly MqttFactory _mqttFactory = new();
    private IMqttClient? _mqttClient;

    public MqttClient(
        IOptions<MqttClientConfiguration> mqttConfig,
        IOptions<MonitorConfiguration> monitorConfiguration,
        ILogger<MqttClient> logger)
    {
        _logger = logger;
        _mqttConfig = mqttConfig.Value;
        _monitorConfiguration = monitorConfiguration.Value;
    }

    public async Task StartLoopAsync(CancellationToken ct)
    {
        _mqttClient = _mqttFactory.CreateMqttClient();

        // TODO: wait for network start
        // TODO: set initial monitor params, based on time? 

        AddMessageReceivedHandler();
        await StartConnectionLoop(ct);
    }

    public async Task StopAsync(CancellationToken ct)
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
    }
    
        private void AddMessageReceivedHandler()
    {
        _mqttClient!.ApplicationMessageReceivedAsync += e =>
        {
            try
            {
                string json = e.ApplicationMessage.ConvertPayloadToString();
                var payload = JsonSerializer.Generic.Utf16.Deserialize<MqttPayload>(json);
                _logger.LogDebug("Received message: {@Payload}", payload);
                
                var commands = new List<string>();
                foreach (var monitor in _monitorConfiguration.Monitors)
                {
                    commands.Add($"/SetValueIfNeeded {monitor.Name} {monitor.Brightness.CmmCode} {ConvertPercentToConfiguredMonitorRange(monitor.Brightness.Min, monitor.Brightness.Max, payload.Brightness)}");
                    // contrast is calculated from brightness
                    commands.Add($"/SetValueIfNeeded {monitor.Name} {monitor.Contrast.CmmCode} {ConvertPercentToConfiguredMonitorRange(monitor.Contrast.Min, monitor.Contrast.Max, payload.Brightness)}");
                    // TODO: add color
                }
                
                _logger.LogDebug("Executing commands: {@Commands}", commands);
                NativeMethods.LaunchProcess($"ControlMyMonitor {string.Join(" ", commands)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing message");
            }

            return Task.CompletedTask;
        };
    }

    private static int ConvertPercentToConfiguredMonitorRange(int min, int max, int percent)
    {
        int range = max - min;
        double multiplied = range * (percent / 100d);
        int rounded = (int)Math.Round(multiplied);
        return min + rounded;
    }

    private async Task StartConnectionLoop(CancellationToken ct)
    {
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttConfig.Address, _mqttConfig.Port)
            .WithCredentials(_mqttConfig.Username, _mqttConfig.Password)
            .Build();

        await Connect(mqttClientOptions, ct);
        
        try
        {
            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        if (await _mqttClient.TryPingAsync(ct))
                        {
                            _logger.LogDebug("MQTT client keeps the connection");
                        }
                        else
                        {
                            _logger.LogWarning("MQTT client not connected, trying to reconnect...");
                            await Connect(mqttClientOptions, ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while connecting");
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_mqttConfig.ReconnectDelaySeconds), ct);
                    }
                }
            }, ct);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("{Name} cancelled", nameof(Worker));
        }
    }

    private async Task Connect(
        MqttClientOptions mqttClientOptions,
        CancellationToken ct)
    {
        MqttClientConnectResult? response = default;
        
        try
        {
            response = await _mqttClient!.ConnectAsync(mqttClientOptions, ct);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("{Name} cancelled", nameof(Worker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while connecting");
        }
        
        if (response?.ResultCode != MqttClientConnectResultCode.Success)
            _logger.LogWarning("Connection status: {Status}", response?.ResultCode);

        _logger.LogInformation("Connected: {@Response}", response);
        
        await SubscribeToTopic(ct);
    }

    private async Task SubscribeToTopic(CancellationToken ct)
    {
        try
        {
            var mqttSubscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(_mqttConfig.Topic))
                .Build();
            
            var response = await _mqttClient!.SubscribeAsync(mqttSubscribeOptions, ct);

            _logger.LogInformation("Connected to a topic {Topic} with response: {@Response}",
                _mqttConfig.Topic, response);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("{Name} cancelled", nameof(Worker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT client can't subscribe to a topic {Topic}", _mqttConfig.Topic);
        }
    }
}

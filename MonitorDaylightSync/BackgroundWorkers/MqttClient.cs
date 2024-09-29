using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;
using MonitorDaylightSync.CommandExecutors;
using MonitorDaylightSync.Configuration;
using MonitorDaylightSync.Dtos;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using SpanJson;

namespace MonitorDaylightSync.BackgroundWorkers;

public class MqttClient : IHostedService
{
    private readonly ILogger<MqttClient> _logger;
    private readonly MqttClientConfiguration _mqttConfig;
    private readonly ICommandExecutor _commandExecutor;

    private readonly MqttFactory _mqttFactory = new();
    private IMqttClient? _mqttClient;

    public MqttClient(
        IOptions<MqttClientConfiguration> mqttConfig,
        ILogger<MqttClient> logger,
        ICommandExecutor commandExecutor)
    {
        _logger = logger;
        _commandExecutor = commandExecutor;
        _mqttConfig = mqttConfig.Value;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("Service started");

        _mqttClient = _mqttFactory.CreateMqttClient();

        while (!NetworkInterface.GetIsNetworkAvailable())
            await Task.Yield();

        AddMessageReceivedHandler(ct);
        await StartConnectionLoop(ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("Disconnecting MQTT client...");

        try
        {
            await _mqttClient.DisconnectAsync(cancellationToken: ct);
            _mqttClient!.Dispose();
            _logger.LogInformation("MQTT client disposed");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disconnecting MQTT client");
        }

        _logger.LogInformation("Service stopped");
    }

    private void AddMessageReceivedHandler(CancellationToken ct)
    {
        _mqttClient!.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                string receivedPayloadAsJson = e.ApplicationMessage.ConvertPayloadToString();
                var monitorData = JsonSerializer.Generic.Utf16.Deserialize<MonitorCommandDto>(receivedPayloadAsJson);
                _logger.LogDebug("Received message: {@MonitorData}", monitorData);

                await _commandExecutor.ExecuteAsync(monitorData, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing message");
            }
        };
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service cancelled");
        }
    }

    private async Task Connect(
        MqttClientOptions mqttClientOptions,
        CancellationToken ct)
    {
        MqttClientConnectResult? response;

        try
        {
            response = await _mqttClient!.ConnectAsync(mqttClientOptions, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service cancelled");
            return;
        }
        catch (MqttCommunicationTimedOutException ex)
        {
            _logger.LogError($"Error while connecting. {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while connecting");
            return;
        }

        if (response?.ResultCode != MqttClientConnectResultCode.Success)
        {
            _logger.LogWarning("Connection status: {Status}", response?.ResultCode);
            return;
        }

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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT client can't subscribe to a topic {Topic}", _mqttConfig.Topic);
        }
    }
}
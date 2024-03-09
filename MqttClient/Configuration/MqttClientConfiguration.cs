namespace MqttClient.Configuration;

public class MqttClientConfiguration
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Address { get; set; }
    public int Port { get; set; }
    public string? Topic { get; set; }
    public short ReconnectDelaySeconds { get; set; }
}
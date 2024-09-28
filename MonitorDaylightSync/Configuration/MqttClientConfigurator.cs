using System.Runtime.CompilerServices;

namespace MonitorDaylightSync.Configuration;

public static class MqttClientConfigurator
{
    public const string ConfigPrefix = "MDS_";
    public static void ConfigureMqttClient(MqttClientConfiguration mqttConfig)
    {
        ConfigureSetting(nameof(mqttConfig.Username), mqttConfig.Username);
        ConfigureSetting(nameof(mqttConfig.Password), mqttConfig.Password);
        ConfigureSetting(nameof(mqttConfig.Address), mqttConfig.Address);
        ConfigureSetting(nameof(mqttConfig.Port), mqttConfig.Port);
        ConfigureSetting(nameof(mqttConfig.Topic), mqttConfig.Topic);
    }

    private static void ConfigureSetting<TValue>(string configKey, TValue currentValue)
    {
        if (currentValue is string stringVal && !string.IsNullOrWhiteSpace(stringVal))
            return;
        
        if (currentValue is int intVal && intVal != default)
            return;
        
        // TODO: more types, throw if not handled?
        
        string newValue;
        do
        {
            Console.Write($"Enter {configKey} value: ");
            newValue = Console.ReadLine() ?? "";
        } while (string.IsNullOrWhiteSpace(newValue));

        string envVarKey = $"{ConfigPrefix}{configKey}";
        Environment.SetEnvironmentVariable(envVarKey, newValue, EnvironmentVariableTarget.User);
    }
}
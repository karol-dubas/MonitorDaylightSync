using System.Collections;
using MonitorDaylightSync.BackgroundWorkers;
using MonitorDaylightSync.CommandExecutors;
using MonitorDaylightSync.Configuration;
using MonitorDaylightSync.Helpers;
using Serilog;
using Serilog.Debugging;

var hostBuilder = Host.CreateApplicationBuilder(args);

if (!hostBuilder.Environment.IsDevelopment())
{
   // The application could have been compiled as WinExe,
   // but then the "Working In Background" cursor shows up during command execution.
   // This is why it's compiled as ConsoleApp (exe) and the console is detached.
   ConsoleHelper.FreeConsole();
}

// Add/override configuration with prefixed environment variables
hostBuilder.Configuration.AddEnvironmentVariables(MqttClientConfigurator.ConfigPrefix);

// Add services
hostBuilder.Services.AddSerilog((sp, config) => config.ReadFrom.Configuration(hostBuilder.Configuration));
SelfLog.Enable(Console.Error); // Write Serilog invalid config to console

var mqttClientConfigSection = hostBuilder.Configuration.GetRequiredSection("MqttClient");
hostBuilder.Services.Configure<MqttClientConfiguration>(o => mqttClientConfigSection.Bind(o));
hostBuilder.Services.Configure<MonitorConfiguration>(o => hostBuilder.Configuration.GetRequiredSection("Monitors").Bind(o));

hostBuilder.Services.AddSingleton<ICommandExecutor, CmmCommandExecutor>();

if (hostBuilder.Environment.IsDevelopment())
   hostBuilder.Services.AddHostedService<TestCommandSender>();

var mqttConfig = mqttClientConfigSection.Get<MqttClientConfiguration>();
if (mqttConfig?.IsEnabled == true)
{
   MqttClientConfigurator.ConfigureMqttClient(mqttConfig);
   hostBuilder.Services.AddHostedService<MqttClient>();
}

var host = hostBuilder.Build();

var logger = host.Services.GetService<ILogger<Program>>();
ArgumentNullException.ThrowIfNull(logger);
logger.LogInformation("Is MQTT Client enabled: {IsEnabled}", mqttConfig?.IsEnabled);

if (hostBuilder.Environment.IsDevelopment())
{
   var prefixedEnvs = Environment.GetEnvironmentVariables()
      .Cast<DictionaryEntry>()
      .Where(x => ((string)x.Key).StartsWith(MqttClientConfigurator.ConfigPrefix))
      .ToDictionary(x => (string)x.Key, x => (string?)x.Value);
   
   logger.LogDebug("Running with '{Prefix}' env vars: {Values}", MqttClientConfigurator.ConfigPrefix, prefixedEnvs);
}

try
{
   logger.LogInformation("Starting app...");
   await host.RunAsync(); // TODO: app deadlock on shutdown in production mode (detached console?)
}
catch (Exception e)
{
   logger.LogCritical(e, "Host terminated unexpectedly");
}
finally
{
   logger.LogInformation("Stopping app...");
   Log.CloseAndFlush();
}

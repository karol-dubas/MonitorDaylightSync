using MonitorDaylightSync.Configuration;
using MonitorDaylightSync.Helpers;
using MonitorDaylightSync.Services;
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

// Add services
{
   hostBuilder.Services.AddSerilog((sp, config) => config.ReadFrom.Configuration(hostBuilder.Configuration));
   SelfLog.Enable(Console.Error); // Write Serilog invalid config to console

   hostBuilder.Services.Configure<MqttClientConfiguration>(o => hostBuilder.Configuration.GetSection("MqttClient").Bind(o));
   hostBuilder.Services.Configure<MonitorConfiguration>(o => hostBuilder.Configuration.GetSection("Monitors").Bind(o));

   hostBuilder.Services.AddSingleton<CmmCommandExecutor>();

   if (hostBuilder.Environment.IsDevelopment())
      hostBuilder.Services.AddHostedService<TestCommandSender>();

   var mqttConfig = hostBuilder.Configuration.GetSection("MqttClient").Get<MqttClientConfiguration>();
   if (mqttConfig?.IsEnabled == true)
       hostBuilder.Services.AddHostedService<MqttClient>();
}

var host = hostBuilder.Build();

var logger = host.Services.GetService<ILogger<Program>>();

try
{
   logger?.LogInformation("Starting app...");
   await host.RunAsync();
}
catch (Exception e)
{
   logger?.LogCritical(e, "Host terminated unexpectedly");
}
finally
{
   logger?.LogInformation("Stopping app...");
   Log.CloseAndFlush();
}

using Microsoft.Extensions.Options;
using MonitorDaylightSync;
using MonitorDaylightSync.Configuration;
using Serilog;
using Serilog.Debugging;

var hostBuilder = Host.CreateDefaultBuilder(args);

// Write Serilog error config to console
SelfLog.Enable(Console.Error);

hostBuilder.UseSerilog((context, config) => config
   .ReadFrom.Configuration(context.Configuration));

hostBuilder.ConfigureServices((context, services) =>
{
   services.Configure<MqttClientConfiguration>(o => context.Configuration.GetSection("MqttClient").Bind(o));
   services.Configure<MonitorConfiguration>(o => context.Configuration.GetSection("Monitors").Bind(o));
   
   services.AddHostedService<MqttClient>();
   services.AddSingleton<CmmCommandExecutor>();
   
   services.AddWindowsService(options => options.ServiceName = nameof(MonitorDaylightSync));
});

var app = hostBuilder.Build();

var logger = app.Services.GetService<ILogger<Program>>();

try
{
   logger?.LogInformation("Starting app...");
   app.Run();
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
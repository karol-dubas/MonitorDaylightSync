using Microsoft.Extensions.Options;
using MonitorDaylightSync;
using MonitorDaylightSync.Configuration;
using Serilog;
using Serilog.Debugging;

// The application could have been compiled as WinExe,
// but then the "Working In Background" cursor shows up during command execution.
// This is why it's compiled as ConsoleApp (exe) and the console is hidden.
if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") != "Development")
   ConsoleHelper.FreeConsole();

var hostBuilder = Host.CreateDefaultBuilder(args);

// Write Serilog error config to console
SelfLog.Enable(Console.Error);

hostBuilder.UseSerilog((context, config) => config
   .ReadFrom.Configuration(context.Configuration));

hostBuilder.ConfigureServices((context, services) =>
{
   services.Configure<MqttClientConfiguration>(o => context.Configuration.GetSection("MqttClient").Bind(o));
   services.Configure<MonitorConfiguration>(o => context.Configuration.GetSection("Monitors").Bind(o));
   
   //services.AddHostedService<MqttClient>(); // TODO: remove
   services.AddSingleton<CmmCommandExecutor>();
});

var app = hostBuilder.Build();

var logger = app.Services.GetService<ILogger<Program>>();

app.Services.GetService<CmmCommandExecutor>(); // TODO: remove

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

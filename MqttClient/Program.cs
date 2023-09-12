using MqttClient;
using Serilog;
using Serilog.Debugging;

var hostBuilder = Host.CreateDefaultBuilder(args);

// Write Serilog error config to console
SelfLog.Enable(Console.Error);

hostBuilder.UseSerilog((context, config) => config
   .ReadFrom.Configuration(context.Configuration));

hostBuilder.ConfigureServices((context, services) =>
{
   var mqttConfig = new MqttClientConfiguration();
   context.Configuration.GetSection("MqttClient").Bind(mqttConfig);
   
   services.AddSingleton(mqttConfig);
   services.AddHostedService<Worker>();
   services.AddWindowsService(options => options.ServiceName = nameof(MqttClient));
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
   // TODO: Env.Exit?
}

// TODO: update log file path to relative

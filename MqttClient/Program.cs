using MqttClient;
using Serilog;
using Serilog.Debugging;

var host = Host.CreateDefaultBuilder(args);

// Write Serilog error config to console
SelfLog.Enable(Console.Error);

host.UseSerilog((context, config) => config
   .ReadFrom.Configuration(context.Configuration));

host.ConfigureServices(services =>
{
   services.AddHostedService<Worker>(); 
});

host.Build().Run();

// TODO: update log file path to relative
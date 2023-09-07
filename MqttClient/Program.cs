using MqttClient;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();

// TODO: logs
// TODO: Program.cs template differences

// TODO: README.md with install instructions
// build & publish to a folder
// `sc create YourServiceName binPath= "C:\Path\To\YourApp\YourApp.exe"`
// `sc start YourServiceName`
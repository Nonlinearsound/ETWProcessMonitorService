using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using App.WindowsService;

using IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "ProcessWatcher";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<WindowsBackgroundService>();
        services.AddHostedService<EventLogService>();
        //services.AddHttpClient<JokeService>();
    })
    .Build();

await host.RunAsync();
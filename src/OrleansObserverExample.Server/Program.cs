using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using OrleansObserverExample.Shared.Interfaces;
using OrleansObserverExample.Server.Grains.Chat;
using OrleansObserverExample.Data.DbContext;
using OrleansObserverExample.Data.Services;
using OrleansObserverExample.Data.Filters;

namespace OrleansObserverExample.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                // 注册数据访问服务
                services.AddSingleton<MockDbContext>();
                services.AddSingleton<IDataService, MockDataService>();
            })
            .UseOrleans(siloBuilder =>
            {
                siloBuilder
                    .UseLocalhostClustering()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "OrleansObserverExample";
                    })
                    .Configure<EndpointOptions>(options =>
                    {
                        options.AdvertisedIPAddress = System.Net.IPAddress.Loopback;
                    })
                    // 添加数据预加载过滤器
                    .AddIncomingGrainCallFilter<DataPreloadFilter>();
            });
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using OrleansObserverExample.Shared.Interfaces;
using OrleansObserverExample.Shared.Models;
using OrleansObserverExample.Client.Observers;

namespace OrleansObserverExample.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await host.StartAsync();

        try
        {
            // 选择运行模式
            Console.WriteLine("请选择运行模式:");
            Console.WriteLine("1. 原始聊天演示");
            Console.WriteLine("2. 任务链演示");
            Console.Write("请输入选择 (1 或 2): ");
            
            var choice = Console.ReadLine();
            
            if (choice == "2")
            {
                await RunTaskChainDemo(host.Services);
            }
            else
            {
                await RunClient(host.Services);
            }
        }
        finally
        {
            await host.StopAsync();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseOrleansClient(clientBuilder =>
            {
                clientBuilder
                    .UseLocalhostClustering()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "OrleansObserverExample";
                    });
            })
            .ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            });

    private static async Task RunClient(IServiceProvider services)
    {
        var client = services.GetRequiredService<IClusterClient>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // 等待客户端连接到集群
        logger.LogInformation("客户端已连接到Orleans集群");

        // 获取聊天Grain引用
        var chatGrain = client.GetGrain<IChatGrain>("main-chat");

        // 创建观察者实例
        var observer1 = new ChatObserver(
            services.GetRequiredService<ILogger<ChatObserver>>(), 
            "客户端1");
        var observer2 = new ChatObserver(
            services.GetRequiredService<ILogger<ChatObserver>>(), 
            "客户端2");

        // 创建观察者引用
        var observerRef1 = client.CreateObjectReference<IChatObserver>(observer1);
        var observerRef2 = client.CreateObjectReference<IChatObserver>(observer2);

        // 订阅观察者
        await chatGrain.Subscribe(observerRef1);
        await chatGrain.Subscribe(observerRef2);

        logger.LogInformation("两个观察者已订阅");

        // 获取当前订阅者数量
        var subscriberCount = await chatGrain.GetSubscriberCount();
        logger.LogInformation("当前订阅者数量: {Count}", subscriberCount);

        // 发送一些测试消息
        await chatGrain.SendMessage("欢迎来到聊天室！");
        await Task.Delay(1000);

        await chatGrain.SendNotification("系统维护将在5分钟后开始");
        await Task.Delay(1000);

        await chatGrain.SendMessage("这是一条测试消息");
        await Task.Delay(1000);

        // 取消一个观察者的订阅
        await chatGrain.Unsubscribe(observerRef1);
        logger.LogInformation("客户端1已取消订阅");

        await chatGrain.SendMessage("客户端1已离开聊天室");
        await Task.Delay(1000);

        // 获取更新后的订阅者数量
        subscriberCount = await chatGrain.GetSubscriberCount();
        logger.LogInformation("当前订阅者数量: {Count}", subscriberCount);

        // 等待一段时间让用户看到结果
        logger.LogInformation("按任意键退出...");
        Console.ReadKey();

        // 取消剩余的订阅
        await chatGrain.Unsubscribe(observerRef2);
        logger.LogInformation("客户端2已取消订阅");
    }

    private static async Task RunTaskChainDemo(IServiceProvider services)
    {
        var client = services.GetRequiredService<IClusterClient>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("=== 任务链系统演示 ===");

        // 创建玩家
        var playerId = "demo-player-" + DateTime.Now.Ticks;
        var playerGrain = client.GetGrain<IPlayerGrain>(playerId);
        await playerGrain.SetName("演示玩家");

        // 创建Observer
        var observer = new ChatObserver(
            services.GetRequiredService<ILogger<ChatObserver>>(), 
            "演示玩家");
        var observerRef = client.CreateObjectReference<IChatObserver>(observer);
        // await playerGrain.SetObserver(observerRef);  

        // 获取任务链
        var newbieChain = client.GetGrain<ITaskChainGrain>("newbie-chain");
        var chatMasterChain = client.GetGrain<ITaskChainGrain>("chat-master-chain");

        // 显示任务链信息
        await ShowTaskChainInfo(newbieChain, "新手任务链");
        await ShowTaskChainInfo(chatMasterChain, "聊天大师挑战");

        // 玩家加入任务链
        logger.LogInformation("\n🎯 玩家加入新手任务链...");
        await playerGrain.JoinTaskChain("newbie-chain");
        await Task.Delay(1000);

        // 模拟完成任务1：加入聊天室
        logger.LogInformation("\n📍 开始完成任务1：加入聊天室");
        await playerGrain.JoinChatRoom("新手村聊天室");
        await Task.Delay(1000);

        // 显示进度
        await ShowPlayerProgress(newbieChain, playerId, "完成任务1后");

        // 模拟完成任务2：发送消息
        logger.LogInformation("\n📍 开始完成任务2：发送10条消息");
        for (int i = 1; i <= 10; i++)
        {
            await playerGrain.SendMessage("新手村聊天室", $"这是第{i}条消息");
            await Task.Delay(200);
        }

        // 显示进度
        await ShowPlayerProgress(newbieChain, playerId, "完成任务2后");

        // 模拟完成任务3：达到等级
        logger.LogInformation("\n📍 开始完成任务3：升级到5级");
        for (int level = 2; level <= 5; level++)
        {
            await playerGrain.SetLevel(level);
            logger.LogInformation($"玩家升级到 {level} 级");
            await Task.Delay(500);
        }

        // 显示最终进度
        await ShowPlayerProgress(newbieChain, playerId, "完成所有任务后");

        // 领取最终奖励
        logger.LogInformation("\n🎁 领取最终奖励...");
        var finalRewards = await newbieChain.ClaimFinalRewards(playerId);
        foreach (var reward in finalRewards)
        {
            logger.LogInformation("🏆 获得最终奖励: {Type} x{Amount} - {Description}", 
                reward.Type, reward.Amount, reward.Description);
        }

        // 显示玩家的奖励历史
        logger.LogInformation("\n📜 玩家奖励历史:");
        var rewardHistory = await playerGrain.GetRewardHistory();
        foreach (var reward in rewardHistory)
        {
            logger.LogInformation("  💰 {Type} x{Amount} - {Description}", 
                reward.Type, reward.Amount, reward.Description);
        }

        logger.LogInformation("\n🎉 任务链演示完成！按任意键退出...");
        Console.ReadKey();
    }

    private static async Task ShowTaskChainInfo(ITaskChainGrain taskChain, string title)
    {
        var chainInfo = await taskChain.GetChainInfo();
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("TaskChainInfo");
        
        logger.LogInformation("\n=== {Title} ===", title);
        logger.LogInformation("📋 任务链ID: {ChainId}", chainInfo.ChainId);
        logger.LogInformation("📝 描述: {Description}", chainInfo.Description);
        logger.LogInformation("📅 开始时间: {StartTime}", chainInfo.StartTime);
        logger.LogInformation("⏰ 结束时间: {EndTime}", chainInfo.EndTime?.ToString() ?? "无限制");
        
        logger.LogInformation("📋 任务列表:");
        foreach (var task in chainInfo.Tasks)
        {
            logger.LogInformation("  {TaskId}. {Name} - {Description} (需要: {Required})", 
                task.TaskId, task.Name, task.Description, task.RequiredCount);
        }
        
        logger.LogInformation("🏆 最终奖励:");
        foreach (var reward in chainInfo.FinalRewards)
        {
            logger.LogInformation("  💰 {Type} x{Amount} - {Description}", 
                reward.Type, reward.Amount, reward.Description);
        }
    }

    private static async Task ShowPlayerProgress(ITaskChainGrain taskChain, string playerId, string title)
    {
        var progress = await taskChain.GetPlayerProgress(playerId);
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Progress");
        
        logger.LogInformation("\n--- {Title} ---", title);
        logger.LogInformation("🎯 当前任务ID: {CurrentTaskId}", progress.CurrentTaskId);
        logger.LogInformation("📊 当前进度: {Progress}", progress.CurrentProgress);
        logger.LogInformation("✅ 已完成任务: [{CompletedTasks}]", string.Join(", ", progress.CompletedTasks));
        logger.LogInformation("🏁 任务链完成: {IsCompleted}", progress.IsChainCompleted ? "是" : "否");
    }
}

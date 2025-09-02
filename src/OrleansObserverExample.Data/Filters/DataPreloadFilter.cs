using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using OrleansObserverExample.Data.Services;
using System.Collections.Concurrent;

namespace OrleansObserverExample.Data.Filters;

/// <summary>
/// 数据预加载过滤器，在 Grain 调用前确保数据已加载
/// </summary>
public class DataPreloadFilter : IIncomingGrainCallFilter
{
    private readonly ILogger<DataPreloadFilter> _logger;
    private readonly IDataService _dataService;
    private static readonly ConcurrentDictionary<string, bool> _initializedServices = new();

    public DataPreloadFilter(ILogger<DataPreloadFilter> logger, IDataService dataService)
    {
        _logger = logger;
        _dataService = dataService;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var grainType = context.Grain.GetType().Name;
        var methodName = context.InterfaceMethod.Name;
        
        _logger.LogDebug("处理 Grain 调用: {GrainType}.{MethodName}", grainType, methodName);

        try
        {
            // 在第一次调用时初始化数据服务
            await EnsureDataServiceInitialized();

            // 继续执行原始的 Grain 调用
            await context.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "在 Grain 调用过滤器中发生错误: {GrainType}.{MethodName}", grainType, methodName);
            throw;
        }
    }

    private async Task EnsureDataServiceInitialized()
    {
        var serviceKey = "DataService";
        
        // 使用双重检查锁定模式确保只初始化一次
        if (!_initializedServices.ContainsKey(serviceKey))
        {
            if (_initializedServices.TryAdd(serviceKey, false))
            {
                _logger.LogInformation("首次调用 Grain，开始初始化数据服务...");
                
                var startTime = DateTime.UtcNow;
                await _dataService.InitializeAsync();
                var duration = DateTime.UtcNow - startTime;
                
                _initializedServices[serviceKey] = true;
                _logger.LogInformation("数据服务初始化完成，耗时: {Duration}ms", duration.TotalMilliseconds);
            }
            else
            {
                // 等待其他线程完成初始化
                while (!_initializedServices.GetValueOrDefault(serviceKey, false))
                {
                    await Task.Delay(10);
                }
            }
        }
    }
}

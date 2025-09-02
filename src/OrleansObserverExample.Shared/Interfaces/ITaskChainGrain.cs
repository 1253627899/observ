using Orleans;
using OrleansObserverExample.Shared.Models;

namespace OrleansObserverExample.Shared.Interfaces;

/// <summary>
/// 任务链Grain接口
/// 每个任务链都是一个独立的Grain实例
/// </summary>
public interface ITaskChainGrain : IGrainWithStringKey
{
    /// <summary>
    /// 获取任务链信息
    /// </summary>
    Task<TaskChainInfo> GetChainInfo();
    
    /// <summary>
    /// 获取玩家在此任务链中的进度
    /// </summary>
    Task<TaskProgress> GetPlayerProgress(string playerId);
    
    /// <summary>
    /// 更新任务进度（通用方法）
    /// </summary>
    Task<TaskCompletionResult> UpdateTaskProgress(string playerId, TaskType taskType, string targetId = "", int count = 1);
    
    /// <summary>
    /// 直接完成指定任务
    /// </summary>
    Task<TaskCompletionResult> CompleteTask(string playerId, int taskId);
    
    /// <summary>
    /// 领取最终奖励
    /// </summary>
    Task<List<Reward>> ClaimFinalRewards(string playerId);
    
    /// <summary>
    /// 重置玩家的任务链进度
    /// </summary>
    Task ResetPlayerProgress(string playerId);
    
    /// <summary>
    /// 获取任务链中所有玩家的进度统计
    /// </summary>
    Task<Dictionary<string, TaskProgress>> GetAllPlayersProgress();
    
    /// <summary>
    /// 检查任务链是否已过期
    /// </summary>
    Task<bool> IsExpired();
}

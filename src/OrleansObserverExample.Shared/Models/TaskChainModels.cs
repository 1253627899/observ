using System;
using System.Collections.Generic;

namespace OrleansObserverExample.Shared.Models;

/// <summary>
/// 任务链信息
/// </summary>
public class TaskChainInfo
{
    public string ChainId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<TaskDefinition> Tasks { get; set; } = new();
    public List<Reward> FinalRewards { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 任务定义
/// </summary>
public class TaskDefinition
{
    public int TaskId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskType Type { get; set; }
    public int RequiredCount { get; set; }
    public List<Reward> Rewards { get; set; } = new();
    public string TargetId { get; set; } = string.Empty; // 目标ID（如怪物ID、物品ID等）
}

/// <summary>
/// 玩家任务进度
/// </summary>
public class TaskProgress
{
    public string PlayerId { get; set; } = string.Empty;
    public string ChainId { get; set; } = string.Empty;
    public int CurrentTaskId { get; set; }
    public int CurrentProgress { get; set; }
    public List<int> CompletedTasks { get; set; } = new();
    public bool IsChainCompleted { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletedTime { get; set; }
}

/// <summary>
/// 奖励信息
/// </summary>
public class Reward
{
    public string Type { get; set; } = string.Empty; // Gold, Exp, Item, Title等
    public int Amount { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 任务类型枚举
/// </summary>
public enum TaskType
{
    /// <summary>
    /// 击杀怪物
    /// </summary>
    KillMonsters = 1,
    
    /// <summary>
    /// 收集物品
    /// </summary>
    CollectItems = 2,
    
    /// <summary>
    /// 达到等级
    /// </summary>
    ReachLevel = 3,
    
    /// <summary>
    /// 完成任务数量
    /// </summary>
    CompleteQuests = 4,
    
    /// <summary>
    /// 加入聊天室
    /// </summary>
    JoinChatRoom = 5,
    
    /// <summary>
    /// 发送消息
    /// </summary>
    SendMessages = 6
}

/// <summary>
/// 任务完成结果
/// </summary>
public class TaskCompletionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Reward> EarnedRewards { get; set; } = new();
    public bool ChainCompleted { get; set; }
    public TaskDefinition? NextTask { get; set; }
}

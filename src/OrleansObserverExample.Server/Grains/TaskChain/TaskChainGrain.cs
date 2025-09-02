using Orleans;
using OrleansObserverExample.Shared.Interfaces;
using OrleansObserverExample.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace OrleansObserverExample.Server.Grains.TaskChain;

/// <summary>
/// 任务链Grain实现类
/// 管理特定任务链的所有逻辑和玩家进度
/// </summary>
public class TaskChainGrain : Grain, ITaskChainGrain
{
    private readonly ILogger<TaskChainGrain> _logger;
    private TaskChainInfo _chainInfo;
    private readonly ConcurrentDictionary<string, TaskProgress> _playerProgress;

    public TaskChainGrain(ILogger<TaskChainGrain> logger)
    {
        _logger = logger;
        _playerProgress = new ConcurrentDictionary<string, TaskProgress>();
        _chainInfo = new TaskChainInfo();
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var chainId = this.GetPrimaryKeyString();
        _chainInfo = await LoadTaskChainDefinition(chainId);
        _logger.LogInformation("TaskChainGrain {ChainId} 已激活，包含 {TaskCount} 个任务", 
            chainId, _chainInfo.Tasks.Count);
        await base.OnActivateAsync(cancellationToken);
    }

    public Task<TaskChainInfo> GetChainInfo()
    {
        return Task.FromResult(_chainInfo);
    }

    public Task<TaskProgress> GetPlayerProgress(string playerId)
    {
        if (!_playerProgress.TryGetValue(playerId, out var progress))
        {
            // 创建新的进度记录
            progress = CreateNewProgress(playerId);
            _playerProgress[playerId] = progress;
            _logger.LogInformation("为玩家 {PlayerId} 创建新的任务链进度 {ChainId}", playerId, _chainInfo.ChainId);
        }
        return Task.FromResult(progress);
    }

    public async Task<TaskCompletionResult> UpdateTaskProgress(string playerId, TaskType taskType, string targetId = "", int count = 1)
    {
        var progress = await GetPlayerProgress(playerId);
        
        if (progress.IsChainCompleted)
        {
            return new TaskCompletionResult 
            { 
                Success = false, 
                Message = "任务链已完成" 
            };
        }

        var currentTask = GetCurrentTask(progress.CurrentTaskId);
        if (currentTask == null)
        {
            return new TaskCompletionResult 
            { 
                Success = false, 
                Message = "当前任务不存在" 
            };
        }

        // 检查任务类型和目标是否匹配
        if (currentTask.Type != taskType)
        {
            return new TaskCompletionResult 
            { 
                Success = false, 
                Message = $"任务类型不匹配，期望 {currentTask.Type}，实际 {taskType}" 
            };
        }

        if (!string.IsNullOrEmpty(currentTask.TargetId) && currentTask.TargetId != targetId)
        {
            return new TaskCompletionResult 
            { 
                Success = false, 
                Message = $"目标不匹配，期望 {currentTask.TargetId}，实际 {targetId}" 
            };
        }

        // 更新进度
        progress.CurrentProgress += count;
        _logger.LogInformation("玩家 {PlayerId} 任务 {TaskId} 进度更新: {Progress}/{Required}", 
            playerId, currentTask.TaskId, progress.CurrentProgress, currentTask.RequiredCount);

        // 检查是否完成当前任务
        if (progress.CurrentProgress >= currentTask.RequiredCount)
        {
            return await CompleteCurrentTask(playerId, progress, currentTask);
        }

        return new TaskCompletionResult 
        { 
            Success = true, 
            Message = $"进度更新成功 ({progress.CurrentProgress}/{currentTask.RequiredCount})" 
        };
    }

    public async Task<TaskCompletionResult> CompleteTask(string playerId, int taskId)
    {
        var progress = await GetPlayerProgress(playerId);
        
        if (progress.CurrentTaskId != taskId)
        {
            return new TaskCompletionResult 
            { 
                Success = false, 
                Message = $"无法完成任务 {taskId}，当前任务是 {progress.CurrentTaskId}" 
            };
        }

        var task = GetCurrentTask(taskId);
        if (task == null)
        {
            return new TaskCompletionResult 
            { 
                Success = false, 
                Message = "任务不存在" 
            };
        }

        // 强制完成任务
        progress.CurrentProgress = task.RequiredCount;
        return await CompleteCurrentTask(playerId, progress, task);
    }

    public async Task<List<Reward>> ClaimFinalRewards(string playerId)
    {
        var progress = await GetPlayerProgress(playerId);
        
        if (!progress.IsChainCompleted)
        {
            _logger.LogWarning("玩家 {PlayerId} 尝试领取未完成任务链 {ChainId} 的最终奖励", playerId, _chainInfo.ChainId);
            return new List<Reward>();
        }

        // 发放最终奖励
        await GiveRewards(playerId, _chainInfo.FinalRewards);
        _logger.LogInformation("玩家 {PlayerId} 领取任务链 {ChainId} 最终奖励", playerId, _chainInfo.ChainId);
        
        return _chainInfo.FinalRewards;
    }

    public Task ResetPlayerProgress(string playerId)
    {
        if (_playerProgress.TryRemove(playerId, out _))
        {
            _logger.LogInformation("重置玩家 {PlayerId} 在任务链 {ChainId} 的进度", playerId, _chainInfo.ChainId);
        }
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, TaskProgress>> GetAllPlayersProgress()
    {
        var allProgress = _playerProgress.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return Task.FromResult(allProgress);
    }

    public Task<bool> IsExpired()
    {
        if (_chainInfo.EndTime.HasValue)
        {
            return Task.FromResult(DateTime.Now > _chainInfo.EndTime.Value);
        }
        return Task.FromResult(false);
    }

    private TaskProgress CreateNewProgress(string playerId)
    {
        var firstTask = _chainInfo.Tasks.OrderBy(t => t.TaskId).FirstOrDefault();
        return new TaskProgress
        {
            PlayerId = playerId,
            ChainId = _chainInfo.ChainId,
            CurrentTaskId = firstTask?.TaskId ?? 0,
            CurrentProgress = 0,
            CompletedTasks = new List<int>(),
            IsChainCompleted = false,
            StartTime = DateTime.Now
        };
    }

    private TaskDefinition? GetCurrentTask(int taskId)
    {
        return _chainInfo.Tasks.FirstOrDefault(t => t.TaskId == taskId);
    }

    private async Task<TaskCompletionResult> CompleteCurrentTask(string playerId, TaskProgress progress, TaskDefinition task)
    {
        // 标记任务完成
        progress.CompletedTasks.Add(task.TaskId);
        
        // 发放任务奖励
        await GiveRewards(playerId, task.Rewards);
        
        _logger.LogInformation("玩家 {PlayerId} 完成任务 {TaskId}: {TaskName}", 
            playerId, task.TaskId, task.Name);

        // 检查是否有下一个任务
        var nextTask = GetNextTask(task.TaskId);
        if (nextTask != null)
        {
            progress.CurrentTaskId = nextTask.TaskId;
            progress.CurrentProgress = 0;
            
            return new TaskCompletionResult
            {
                Success = true,
                Message = $"任务 '{task.Name}' 完成！开始下一个任务: '{nextTask.Name}'",
                EarnedRewards = task.Rewards,
                ChainCompleted = false,
                NextTask = nextTask
            };
        }
        else
        {
            // 任务链完成
            progress.IsChainCompleted = true;
            progress.CompletedTime = DateTime.Now;
            
            // 通知玩家完成任务链
            await NotifyPlayerChainCompleted(playerId);
            
            return new TaskCompletionResult
            {
                Success = true,
                Message = $"恭喜！任务链 '{_chainInfo.Name}' 全部完成！",
                EarnedRewards = task.Rewards,
                ChainCompleted = true
            };
        }
    }

    private TaskDefinition? GetNextTask(int currentTaskId)
    {
        var sortedTasks = _chainInfo.Tasks.OrderBy(t => t.TaskId).ToList();
        var currentIndex = sortedTasks.FindIndex(t => t.TaskId == currentTaskId);
        
        if (currentIndex >= 0 && currentIndex < sortedTasks.Count - 1)
        {
            return sortedTasks[currentIndex + 1];
        }
        return null;
    }

    private async Task GiveRewards(string playerId, List<Reward> rewards)
    {
        var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(playerId);
        foreach (var reward in rewards)
        {
            await playerGrain.AddReward(reward);
        }
    }

    private async Task NotifyPlayerChainCompleted(string playerId)
    {
        var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(playerId);
        await playerGrain.NotifyTaskChainCompleted(_chainInfo.ChainId);
    }

    private async Task<TaskChainInfo> LoadTaskChainDefinition(string chainId)
    {
        // 根据chainId加载不同的任务链定义
        // 这里可以从数据库或配置文件加载，现在用硬编码示例
        
        return chainId switch
        {
            "newbie-chain" => CreateNewbieChain(),
            "chat-master-chain" => CreateChatMasterChain(),
            "social-butterfly-chain" => CreateSocialButterflyChain(),
            _ => CreateDefaultChain(chainId)
        };
    }

    private TaskChainInfo CreateNewbieChain()
    {
        return new TaskChainInfo
        {
            ChainId = "newbie-chain",
            Name = "新手礼包任务链",
            Description = "完成新手引导，获得丰厚奖励！",
            StartTime = DateTime.Now.AddDays(-30), // 30天前开始
            EndTime = DateTime.Now.AddDays(30),    // 30天后结束
            Tasks = new List<TaskDefinition>
            {
                new TaskDefinition
                {
                    TaskId = 1,
                    Name = "加入你的第一个聊天室",
                    Description = "加入任意一个聊天室开始社交",
                    Type = TaskType.JoinChatRoom,
                    RequiredCount = 1,
                    TargetId = "", // 任意聊天室
                    Rewards = new List<Reward>
                    {
                        new Reward { Type = "Gold", Amount = 100, Description = "新手奖励金币" },
                        new Reward { Type = "Exp", Amount = 50, Description = "经验值" }
                    }
                },
                new TaskDefinition
                {
                    TaskId = 2,
                    Name = "发送10条消息",
                    Description = "在聊天室中发送10条消息，开始交流",
                    Type = TaskType.SendMessages,
                    RequiredCount = 10,
                    Rewards = new List<Reward>
                    {
                        new Reward { Type = "Gold", Amount = 200, Description = "活跃奖励" },
                        new Reward { Type = "Title", Title = "话痨", Description = "聊天达人称号" }
                    }
                },
                new TaskDefinition
                {
                    TaskId = 3,
                    Name = "达到5级",
                    Description = "将角色等级提升到5级",
                    Type = TaskType.ReachLevel,
                    RequiredCount = 5,
                    Rewards = new List<Reward>
                    {
                        new Reward { Type = "Gold", Amount = 500, Description = "等级奖励" },
                        new Reward { Type = "Item", ItemId = "SuperChatBadge", Amount = 1, Description = "超级聊天徽章" }
                    }
                }
            },
            FinalRewards = new List<Reward>
            {
                new Reward { Type = "Gold", Amount = 1000, Description = "任务链完成奖励" },
                new Reward { Type = "Item", ItemId = "NewbieGraduateCertificate", Amount = 1, Description = "新手毕业证书" },
                new Reward { Type = "Title", Title = "社交新星", Description = "新手毕业生专属称号" }
            }
        };
    }

    private TaskChainInfo CreateChatMasterChain()
    {
        return new TaskChainInfo
        {
            ChainId = "chat-master-chain",
            Name = "聊天大师挑战",
            Description = "成为真正的聊天大师！",
            StartTime = DateTime.Now.AddDays(-7),
            EndTime = DateTime.Now.AddDays(7),
            Tasks = new List<TaskDefinition>
            {
                new TaskDefinition
                {
                    TaskId = 1,
                    Name = "加入3个不同的聊天室",
                    Description = "体验不同聊天室的氛围",
                    Type = TaskType.JoinChatRoom,
                    RequiredCount = 3,
                    Rewards = new List<Reward>
                    {
                        new Reward { Type = "Gold", Amount = 300, Description = "探索奖励" }
                    }
                },
                new TaskDefinition
                {
                    TaskId = 2,
                    Name = "发送100条消息",
                    Description = "在聊天室中发送100条有意义的消息",
                    Type = TaskType.SendMessages,
                    RequiredCount = 100,
                    Rewards = new List<Reward>
                    {
                        new Reward { Type = "Gold", Amount = 1000, Description = "活跃大师奖励" },
                        new Reward { Type = "Title", Title = "聊天达人", Description = "聊天大师称号" }
                    }
                }
            },
            FinalRewards = new List<Reward>
            {
                new Reward { Type = "Gold", Amount = 2000, Description = "聊天大师奖励" },
                new Reward { Type = "Title", Title = "聊天大师", Description = "聊天大师专属称号" }
            }
        };
    }

    private TaskChainInfo CreateSocialButterflyChain()
    {
        return new TaskChainInfo
        {
            ChainId = "social-butterfly-chain",
            Name = "社交蝴蝶",
            Description = "像蝴蝶一样在各个聊天室间飞舞",
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddDays(14),
            Tasks = new List<TaskDefinition>
            {
                new TaskDefinition
                {
                    TaskId = 1,
                    Name = "加入5个聊天室",
                    Description = "成为5个不同聊天室的成员",
                    Type = TaskType.JoinChatRoom,
                    RequiredCount = 5,
                    Rewards = new List<Reward>
                    {
                        new Reward { Type = "Gold", Amount = 500, Description = "社交奖励" }
                    }
                },
                new TaskDefinition
                {
                    TaskId = 2,
                    Name = "达到10级",
                    Description = "将等级提升到10级",
                    Type = TaskType.ReachLevel,
                    RequiredCount = 10,
                    Rewards = new List<Reward>
                    {
                        new Reward { Type = "Gold", Amount = 1500, Description = "等级成就奖励" }
                    }
                }
            },
            FinalRewards = new List<Reward>
            {
                new Reward { Type = "Gold", Amount = 3000, Description = "社交蝴蝶奖励" },
                new Reward { Type = "Title", Title = "社交蝴蝶", Description = "社交达人专属称号" }
            }
        };
    }

    private TaskChainInfo CreateDefaultChain(string chainId)
    {
        return new TaskChainInfo
        {
            ChainId = chainId,
            Name = $"自定义任务链 - {chainId}",
            Description = "这是一个示例任务链",
            StartTime = DateTime.Now,
            Tasks = new List<TaskDefinition>
            {
                new TaskDefinition
                {
                    TaskId = 1,
                    Name = "示例任务",
                    Description = "这是一个示例任务",
                    Type = TaskType.SendMessages,
                    RequiredCount = 1,
                    Rewards = new List<Reward>
                    {
                        new Reward { Type = "Gold", Amount = 100, Description = "示例奖励" }
                    }
                }
            },
            FinalRewards = new List<Reward>
            {
                new Reward { Type = "Gold", Amount = 500, Description = "完成奖励" }
            }
        };
    }
}

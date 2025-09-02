using Orleans;
using OrleansObserverExample.Shared.Interfaces;
using OrleansObserverExample.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace OrleansObserverExample.Server.Grains.Player;

/// <summary>
/// 玩家Grain实现类
/// 每个玩家都是一个独立的Grain实例，管理玩家的状态和行为
/// </summary>
public class PlayerGrain : Grain, IPlayerGrain
{
    private readonly ILogger<PlayerGrain> _logger;
    private PlayerInfo _playerInfo;
    private IChatObserver? _observer;
    private readonly HashSet<string> _joinedRooms = new();
    private readonly List<Reward> _rewardHistory = new();
    private readonly HashSet<string> _joinedTaskChains = new();
    private int _level = 1;

    public PlayerGrain(ILogger<PlayerGrain> logger)
    {
        _logger = logger;
        _playerInfo = new PlayerInfo();
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _playerInfo.Id = this.GetPrimaryKeyString();
        _playerInfo.JoinTime = DateTime.Now;
        _logger.LogInformation("PlayerGrain {PlayerId} 已激活", _playerInfo.Id);
        await base.OnActivateAsync(cancellationToken);
    }

    public Task<string> GetName()
    {
        return Task.FromResult(_playerInfo.Name);
    }

    public Task SetName(string name)
    {
        _playerInfo.Name = name;
        _logger.LogInformation("玩家 {PlayerId} 设置名称为: {Name}", _playerInfo.Id, name);
        return Task.CompletedTask;
    }

    public Task<PlayerInfo> GetPlayerInfo()
    {
        var info = new PlayerInfo
        {
            Id = _playerInfo.Id,
            Name = _playerInfo.Name,
            JoinTime = _playerInfo.JoinTime,
            JoinedRooms = _joinedRooms.ToList(),
            IsOnline = _observer != null
        };
        return Task.FromResult(info);
    }

    public async Task JoinChatRoom(string chatRoomId)
    {
        if (_joinedRooms.Contains(chatRoomId))
        {
            _logger.LogWarning("玩家 {PlayerId} 已经在聊天室 {ChatRoomId} 中", _playerInfo.Id, chatRoomId);
            return;
        }

        var chatGrain = GrainFactory.GetGrain<IChatGrain>(chatRoomId);
        await chatGrain.PlayerJoin(_playerInfo.Id, _playerInfo.Name);
        
        _joinedRooms.Add(chatRoomId);
        _logger.LogInformation("玩家 {PlayerId} 加入聊天室 {ChatRoomId}", _playerInfo.Id, chatRoomId);
        
        // 更新任务进度
        await UpdateTaskProgress(TaskType.JoinChatRoom, chatRoomId, 1);
    }

    public async Task LeaveChatRoom(string chatRoomId)
    {
        if (!_joinedRooms.Contains(chatRoomId))
        {
            _logger.LogWarning("玩家 {PlayerId} 不在聊天室 {ChatRoomId} 中", _playerInfo.Id, chatRoomId);
            return;
        }

        var chatGrain = GrainFactory.GetGrain<IChatGrain>(chatRoomId);
        await chatGrain.PlayerLeave(_playerInfo.Id);
        
        _joinedRooms.Remove(chatRoomId);
        _logger.LogInformation("玩家 {PlayerId} 离开聊天室 {ChatRoomId}", _playerInfo.Id, chatRoomId);
    }

    public Task<List<string>> GetJoinedRooms()
    {
        return Task.FromResult(_joinedRooms.ToList());
    }

    public async Task SendMessage(string chatRoomId, string message)
    {
        if (!_joinedRooms.Contains(chatRoomId))
        {
            _logger.LogWarning("玩家 {PlayerId} 不在聊天室 {ChatRoomId} 中，无法发送消息", _playerInfo.Id, chatRoomId);
            return;
        }

        var chatGrain = GrainFactory.GetGrain<IChatGrain>(chatRoomId);
        await chatGrain.SendPlayerMessage(_playerInfo.Id, _playerInfo.Name, message);
        
        _logger.LogInformation("玩家 {PlayerId} 在聊天室 {ChatRoomId} 发送消息: {Message}", 
            _playerInfo.Id, chatRoomId, message);
        
        // 更新任务进度
        await UpdateTaskProgress(TaskType.SendMessages, "", 1);
    }

    public async Task SendPrivateMessage(string targetPlayerId, string message)
    {
        var targetPlayer = GrainFactory.GetGrain<IPlayerGrain>(targetPlayerId);
        var targetPlayerInfo = await targetPlayer.GetPlayerInfo();
        
        if (!targetPlayerInfo.IsOnline)
        {
            _logger.LogWarning("目标玩家 {TargetPlayerId} 不在线，无法发送私信", targetPlayerId);
            return;
        }

        // 这里可以实现私信逻辑，暂时记录日志
        _logger.LogInformation("玩家 {PlayerId} 向 {TargetPlayerId} 发送私信: {Message}", 
            _playerInfo.Id, targetPlayerId, message);
    }

    public Task SetObserver(IChatObserver observer)
    {
        _observer = observer;
        _playerInfo.IsOnline = true;
        _logger.LogInformation("玩家 {PlayerId} 设置了观察者，现在在线", _playerInfo.Id);
        return Task.CompletedTask;
    }

    public Task RemoveObserver()
    {
        _observer = null;
        _playerInfo.IsOnline = false;
        _logger.LogInformation("玩家 {PlayerId} 移除了观察者，现在离线", _playerInfo.Id);
        return Task.CompletedTask;
    }

    public async Task GoOnline()
    {
        _playerInfo.IsOnline = true;
        _logger.LogInformation("玩家 {PlayerId} 上线", _playerInfo.Id);
        
        // 通知所有加入的聊天室
        foreach (var roomId in _joinedRooms)
        {
            var chatGrain = GrainFactory.GetGrain<IChatGrain>(roomId);
            await chatGrain.SendNotification($"{_playerInfo.Name} 上线了");
        }
    }

    public async Task GoOffline()
    {
        _playerInfo.IsOnline = false;
        _logger.LogInformation("玩家 {PlayerId} 下线", _playerInfo.Id);
        
        // 通知所有加入的聊天室
        foreach (var roomId in _joinedRooms)
        {
            var chatGrain = GrainFactory.GetGrain<IChatGrain>(roomId);
            await chatGrain.SendNotification($"{_playerInfo.Name} 下线了");
        }
    }

    public Task AddReward(Reward reward)
    {
        _rewardHistory.Add(reward);
        _logger.LogInformation("玩家 {PlayerId} 获得奖励: {Type} x{Amount} - {Description}", 
            _playerInfo.Id, reward.Type, reward.Amount, reward.Description);
        return Task.CompletedTask;
    }

    public Task<List<Reward>> GetRewardHistory()
    {
        return Task.FromResult(_rewardHistory.ToList());
    }

    public Task NotifyTaskChainCompleted(string chainId)
    {
        _logger.LogInformation("玩家 {PlayerId} 完成任务链 {ChainId}", _playerInfo.Id, chainId);
        
        // 这里可以触发特殊效果、发送通知等
        if (_observer != null)
        {
            _observer.ReceiveNotification($"🎉 恭喜完成任务链: {chainId}！");
        }
        
        return Task.CompletedTask;
    }

    public Task SetLevel(int level)
    {
        var oldLevel = _level;
        _level = level;
        _logger.LogInformation("玩家 {PlayerId} 等级从 {OldLevel} 提升到 {NewLevel}", 
            _playerInfo.Id, oldLevel, level);
        
        // 触发等级相关的任务进度更新
        _ = Task.Run(async () =>
        {
            await UpdateTaskProgress(TaskType.ReachLevel, level.ToString(), 1);
        });
        
        return Task.CompletedTask;
    }

    public Task<int> GetLevel()
    {
        return Task.FromResult(_level);
    }

    public Task<List<string>> GetJoinedTaskChains()
    {
        return Task.FromResult(_joinedTaskChains.ToList());
    }

    public async Task JoinTaskChain(string chainId)
    {
        if (_joinedTaskChains.Add(chainId))
        {
            _logger.LogInformation("玩家 {PlayerId} 加入任务链 {ChainId}", _playerInfo.Id, chainId);
            
            // 获取任务链Grain并初始化进度
            var taskChainGrain = GrainFactory.GetGrain<ITaskChainGrain>(chainId);
            await taskChainGrain.GetPlayerProgress(_playerInfo.Id); // 这会自动创建进度记录
        }
    }



    private async Task UpdateTaskProgress(TaskType taskType, string targetId, int count)
    {
        // 更新所有已加入的任务链的进度
        var tasks = _joinedTaskChains.Select(async chainId =>
        {
            try
            {
                var taskChainGrain = GrainFactory.GetGrain<ITaskChainGrain>(chainId);
                var result = await taskChainGrain.UpdateTaskProgress(_playerInfo.Id, taskType, targetId, count);
                
                if (result.Success && _observer != null)
                {
                    _observer.ReceiveNotification($"📋 {result.Message}");
                    
                    if (result.ChainCompleted)
                    {
                        _observer.ReceiveNotification($"🎊 任务链完成！可以领取最终奖励了！");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新任务链 {ChainId} 进度时出错", chainId);
            }
        });
        
        await Task.WhenAll(tasks);
    }
}

using Orleans;
using OrleansObserverExample.Shared.Interfaces;
using OrleansObserverExample.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using OrleansObserverExample.Data.Services;
using OrleansObserverExample.Data.Models;

namespace OrleansObserverExample.Server.Grains.Chat;

/// <summary>
/// 聊天Grain实现类
/// 使用ObserverSubscriptionManager来管理观察者订阅
/// </summary>
public class ChatGrain : Grain, IChatGrain
{
    private readonly ConcurrentDictionary<IChatObserver, bool> _subscribers;
    private readonly ConcurrentDictionary<string, PlayerInfo> _players;
    private readonly ILogger<ChatGrain> _logger;
    private readonly IDataService _dataService;

    public ChatGrain(ILogger<ChatGrain> logger, IDataService dataService)
    {
        _logger = logger;
        _dataService = dataService;
        _subscribers = new ConcurrentDictionary<IChatObserver, bool>();
        _players = new ConcurrentDictionary<string, PlayerInfo>();
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        _logger.LogInformation("ChatGrain {GrainId} 已激活", grainId);
        
        // 从数据服务加载聊天室信息
        var chatRoom = await _dataService.GetChatRoomAsync(grainId);
        if (chatRoom != null)
        {
            _logger.LogInformation("加载聊天室数据: {RoomName}, 成员数量: {MemberCount}", 
                chatRoom.Name, chatRoom.Members.Count);
                
            // 加载聊天室的历史消息
            var messages = await _dataService.GetRoomMessagesAsync(grainId, 10);
            _logger.LogInformation("加载了 {MessageCount} 条历史消息", messages.Count());
        }
        else
        {
            _logger.LogWarning("未找到聊天室数据: {GrainId}", grainId);
        }
        
        await base.OnActivateAsync(cancellationToken);
    }

    public Task Subscribe(IChatObserver observer)
    {
        if (_subscribers.TryAdd(observer, true))
        {
            _logger.LogInformation("新的观察者已订阅，当前订阅者数量: {Count}", _subscribers.Count);
        }
        else
        {
            _logger.LogWarning("观察者已经订阅过了");
        }
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IChatObserver observer)
    {
        if (_subscribers.TryRemove(observer, out _))
        {
            _logger.LogInformation("观察者已取消订阅，当前订阅者数量: {Count}", _subscribers.Count);
        }
        else
        {
            _logger.LogWarning("观察者未订阅，无法取消订阅");
        }
        return Task.CompletedTask;
    }

    public async Task SendMessage(string message)
    {
        _logger.LogInformation("发送消息: {Message}", message);
        
        // 保存消息到数据服务
        var messageEntity = new Message
        {
            Id = Guid.NewGuid().ToString(),
            ChatRoomId = this.GetPrimaryKeyString(),
            SenderId = "system", // 这里可以从上下文获取真实的发送者ID
            SenderName = "系统",
            Content = message,
            Type = MessageType.Text,
            SentAt = DateTime.UtcNow,
            IsDeleted = false
        };
        
        await _dataService.AddMessageAsync(messageEntity);
        
        // 向所有订阅的观察者发送消息
        var tasks = _subscribers.Keys.Select(observer => 
        {
            try
            {
                observer.ReceiveMessage(message);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向观察者发送消息时出错");
                return Task.CompletedTask;
            }
        }).ToArray();
        
        await Task.WhenAll(tasks);
    }

    public Task SendNotification(string notification)
    {
        _logger.LogInformation("发送系统通知: {Notification}", notification);
        
        // 向所有订阅的观察者发送系统通知
        var tasks = _subscribers.Keys.Select(observer => 
        {
            try
            {
                observer.ReceiveNotification(notification);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向观察者发送系统通知时出错");
                return Task.CompletedTask;
            }
        }).ToArray();
        
        return Task.WhenAll(tasks);
    }

    public Task<int> GetSubscriberCount()
    {
        var count = _subscribers.Count;
        _logger.LogInformation("当前订阅者数量: {Count}", count);
        return Task.FromResult(count);
    }

    public Task PlayerJoin(string playerId, string playerName)
    {
        var playerInfo = new PlayerInfo
        {
            Id = playerId,
            Name = playerName,
            JoinTime = DateTime.Now,
            IsOnline = true
        };

        if (_players.TryAdd(playerId, playerInfo))
        {
            _logger.LogInformation("玩家 {PlayerName} ({PlayerId}) 加入聊天室 {ChatRoomId}", 
                playerName, playerId, this.GetPrimaryKeyString());
            
            // 通知其他玩家有新玩家加入
            var joinMessage = $"🎉 {playerName} 加入了聊天室";
            return SendNotification(joinMessage);
        }
        else
        {
            _logger.LogWarning("玩家 {PlayerId} 已经在聊天室中", playerId);
            return Task.CompletedTask;
        }
    }

    public Task PlayerLeave(string playerId)
    {
        if (_players.TryRemove(playerId, out var playerInfo))
        {
            _logger.LogInformation("玩家 {PlayerName} ({PlayerId}) 离开聊天室 {ChatRoomId}", 
                playerInfo.Name, playerId, this.GetPrimaryKeyString());
            
            // 通知其他玩家有玩家离开
            var leaveMessage = $"👋 {playerInfo.Name} 离开了聊天室";
            return SendNotification(leaveMessage);
        }
        else
        {
            _logger.LogWarning("玩家 {PlayerId} 不在聊天室中", playerId);
            return Task.CompletedTask;
        }
    }

    public Task<List<PlayerInfo>> GetOnlinePlayers()
    {
        var onlinePlayers = _players.Values.Where(p => p.IsOnline).ToList();
        _logger.LogInformation("当前聊天室 {ChatRoomId} 在线玩家数量: {Count}", 
            this.GetPrimaryKeyString(), onlinePlayers.Count);
        return Task.FromResult(onlinePlayers);
    }

    public Task SendPlayerMessage(string playerId, string playerName, string message)
    {
        if (_players.ContainsKey(playerId))
        {
            var formattedMessage = $"{playerName}: {message}";
            _logger.LogInformation("玩家 {PlayerName} 在聊天室 {ChatRoomId} 发送消息: {Message}", 
                playerName, this.GetPrimaryKeyString(), message);
            
            return SendMessage(formattedMessage);
        }
        else
        {
            _logger.LogWarning("玩家 {PlayerId} 不在聊天室中，无法发送消息", playerId);
            return Task.CompletedTask;
        }
    }
}

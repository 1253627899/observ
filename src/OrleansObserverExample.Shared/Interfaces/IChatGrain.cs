using Orleans;
using OrleansObserverExample.Shared.Models;

namespace OrleansObserverExample.Shared.Interfaces;

/// <summary>
/// 聊天Grain接口
/// </summary>
public interface IChatGrain : IGrainWithStringKey
{
    /// <summary>
    /// 订阅聊天通知
    /// </summary>
    /// <param name="observer">观察者引用</param>
    /// <returns>订阅结果</returns>
    Task Subscribe(IChatObserver observer);
    
    /// <summary>
    /// 取消订阅聊天通知
    /// </summary>
    /// <param name="observer">观察者引用</param>
    /// <returns>取消订阅结果</returns>
    Task Unsubscribe(IChatObserver observer);
    
    /// <summary>
    /// 发送聊天消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <returns>发送结果</returns>
    Task SendMessage(string message);
    
    /// <summary>
    /// 发送系统通知
    /// </summary>
    /// <param name="notification">通知内容</param>
    /// <returns>发送结果</returns>
    Task SendNotification(string notification);
    
    /// <summary>
    /// 获取当前订阅者数量
    /// </summary>
    /// <returns>订阅者数量</returns>
    Task<int> GetSubscriberCount();
    
    /// <summary>
    /// 玩家加入聊天室
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="playerName">玩家姓名</param>
    /// <returns>加入结果</returns>
    Task PlayerJoin(string playerId, string playerName);
    
    /// <summary>
    /// 玩家离开聊天室
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <returns>离开结果</returns>
    Task PlayerLeave(string playerId);
    
    /// <summary>
    /// 获取在线玩家列表
    /// </summary>
    /// <returns>在线玩家信息列表</returns>
    Task<List<PlayerInfo>> GetOnlinePlayers();
    
    /// <summary>
    /// 玩家发送消息
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="playerName">玩家姓名</param>
    /// <param name="message">消息内容</param>
    /// <returns>发送结果</returns>
    Task SendPlayerMessage(string playerId, string playerName, string message);
}

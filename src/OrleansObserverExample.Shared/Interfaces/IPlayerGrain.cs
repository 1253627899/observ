using Orleans;
using OrleansObserverExample.Shared.Models;

namespace OrleansObserverExample.Shared.Interfaces;

/// <summary>
/// 玩家Grain接口
/// 每个玩家都是一个独立的Grain实例
/// </summary>
public interface IPlayerGrain : IGrainWithStringKey
{
    /// <summary>
    /// 获取玩家姓名
    /// </summary>
    Task<string> GetName();
    
    /// <summary>
    /// 设置玩家姓名
    /// </summary>
    Task SetName(string name);
    
    /// <summary>
    /// 获取玩家完整信息
    /// </summary>
    Task<PlayerInfo> GetPlayerInfo();
    
    /// <summary>
    /// 加入聊天室
    /// </summary>
    Task JoinChatRoom(string chatRoomId);
    
    /// <summary>
    /// 离开聊天室
    /// </summary>
    Task LeaveChatRoom(string chatRoomId);
    
    /// <summary>
    /// 获取已加入的聊天室列表
    /// </summary>
    Task<List<string>> GetJoinedRooms();
    
    /// <summary>
    /// 在指定聊天室发送消息
    /// </summary>
    Task SendMessage(string chatRoomId, string message);
    
    /// <summary>
    /// 发送私人消息给其他玩家
    /// </summary>
    Task SendPrivateMessage(string targetPlayerId, string message);
    
    /// <summary>
    /// 设置消息观察者
    /// </summary>
    Task SetObserver(IChatObserver observer);
    
    /// <summary>
    /// 移除消息观察者
    /// </summary>
    Task RemoveObserver();
    
    /// <summary>
    /// 玩家上线
    /// </summary>
    Task GoOnline();
    
    /// <summary>
    /// 玩家下线
    /// </summary>
    Task GoOffline();
    
    /// <summary>
    /// 添加奖励到玩家背包
    /// </summary>
    Task AddReward(Reward reward);
    
    /// <summary>
    /// 获取玩家的奖励历史
    /// </summary>
    Task<List<Reward>> GetRewardHistory();
    
    /// <summary>
    /// 通知玩家任务链完成
    /// </summary>
    Task NotifyTaskChainCompleted(string chainId);
    
    /// <summary>
    /// 更新玩家等级
    /// </summary>
    Task SetLevel(int level);
    
    /// <summary>
    /// 获取玩家等级
    /// </summary>
    Task<int> GetLevel();
    
    /// <summary>
    /// 获取玩家参与的所有任务链
    /// </summary>
    Task<List<string>> GetJoinedTaskChains();
    
    /// <summary>
    /// 加入任务链
    /// </summary>
    Task JoinTaskChain(string chainId);
}

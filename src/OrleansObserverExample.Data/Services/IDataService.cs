using OrleansObserverExample.Data.Models;

namespace OrleansObserverExample.Data.Services;

/// <summary>
/// 数据访问服务接口
/// </summary>
public interface IDataService
{
    /// <summary>
    /// 初始化数据服务
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 获取用户信息
    /// </summary>
    Task<User?> GetUserAsync(string userId);

    /// <summary>
    /// 获取所有活跃用户
    /// </summary>
    Task<IEnumerable<User>> GetActiveUsersAsync();

    /// <summary>
    /// 获取聊天室信息
    /// </summary>
    Task<ChatRoom?> GetChatRoomAsync(string roomId);

    /// <summary>
    /// 获取用户参与的聊天室
    /// </summary>
    Task<IEnumerable<ChatRoom>> GetUserChatRoomsAsync(string userId);

    /// <summary>
    /// 获取聊天室的历史消息
    /// </summary>
    Task<IEnumerable<Message>> GetRoomMessagesAsync(string roomId, int limit = 50);

    /// <summary>
    /// 添加新消息
    /// </summary>
    Task<bool> AddMessageAsync(Message message);

    /// <summary>
    /// 更新用户最后登录时间
    /// </summary>
    Task UpdateUserLastLoginAsync(string userId);

    /// <summary>
    /// 检查用户是否存在
    /// </summary>
    Task<bool> UserExistsAsync(string userId);

    /// <summary>
    /// 检查聊天室是否存在
    /// </summary>
    Task<bool> ChatRoomExistsAsync(string roomId);
}

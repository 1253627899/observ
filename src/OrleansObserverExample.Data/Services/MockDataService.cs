using Microsoft.Extensions.Logging;
using OrleansObserverExample.Data.DbContext;
using OrleansObserverExample.Data.Models;

namespace OrleansObserverExample.Data.Services;

/// <summary>
/// 模拟数据访问服务实现
/// </summary>
public class MockDataService : IDataService
{
    private readonly MockDbContext _dbContext;
    private readonly ILogger<MockDataService> _logger;

    public MockDataService(MockDbContext dbContext, ILogger<MockDataService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("初始化数据服务...");
        await _dbContext.InitializeAsync();
        _logger.LogInformation("数据服务初始化完成");
    }

    public Task<User?> GetUserAsync(string userId)
    {
        _logger.LogDebug("获取用户信息: {UserId}", userId);
        var user = _dbContext.GetUser(userId);
        return Task.FromResult(user);
    }

    public Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        _logger.LogDebug("获取所有活跃用户");
        var activeUsers = _dbContext.Users.Where(u => u.IsActive);
        return Task.FromResult(activeUsers);
    }

    public Task<ChatRoom?> GetChatRoomAsync(string roomId)
    {
        _logger.LogDebug("获取聊天室信息: {RoomId}", roomId);
        var room = _dbContext.GetChatRoom(roomId);
        return Task.FromResult(room);
    }

    public Task<IEnumerable<ChatRoom>> GetUserChatRoomsAsync(string userId)
    {
        _logger.LogDebug("获取用户参与的聊天室: {UserId}", userId);
        var userRooms = _dbContext.ChatRooms.Where(r => r.Members.Contains(userId) && r.IsActive);
        return Task.FromResult(userRooms);
    }

    public Task<IEnumerable<Message>> GetRoomMessagesAsync(string roomId, int limit = 50)
    {
        _logger.LogDebug("获取聊天室历史消息: {RoomId}, 限制: {Limit}", roomId, limit);
        var messages = _dbContext.GetMessagesForRoom(roomId).TakeLast(limit);
        return Task.FromResult(messages);
    }

    public async Task<bool> AddMessageAsync(Message message)
    {
        _logger.LogDebug("添加新消息: {MessageId} 在聊天室 {RoomId}", message.Id, message.ChatRoomId);
        
        var added = _dbContext.AddMessage(message);
        if (added)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("成功添加消息: {MessageId}", message.Id);
        }
        else
        {
            _logger.LogWarning("添加消息失败，消息ID可能已存在: {MessageId}", message.Id);
        }
        
        return added;
    }

    public async Task UpdateUserLastLoginAsync(string userId)
    {
        _logger.LogDebug("更新用户最后登录时间: {UserId}", userId);
        
        var user = _dbContext.GetUser(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            _logger.LogDebug("已更新用户 {UserId} 的最后登录时间", userId);
        }
        else
        {
            _logger.LogWarning("尝试更新不存在用户的登录时间: {UserId}", userId);
        }
    }

    public Task<bool> UserExistsAsync(string userId)
    {
        var exists = _dbContext.GetUser(userId) != null;
        _logger.LogDebug("检查用户是否存在: {UserId} = {Exists}", userId, exists);
        return Task.FromResult(exists);
    }

    public Task<bool> ChatRoomExistsAsync(string roomId)
    {
        var exists = _dbContext.GetChatRoom(roomId) != null;
        _logger.LogDebug("检查聊天室是否存在: {RoomId} = {Exists}", roomId, exists);
        return Task.FromResult(exists);
    }
}

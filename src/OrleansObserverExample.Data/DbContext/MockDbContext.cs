using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OrleansObserverExample.Data.Models;
using System.Collections.Concurrent;
using System.Reflection;

namespace OrleansObserverExample.Data.DbContext;

/// <summary>
/// 模拟数据库上下文，用于替代真实的数据库连接
/// </summary>
public class MockDbContext : IDisposable
{
    private readonly ILogger<MockDbContext> _logger;
    private readonly ConcurrentDictionary<string, User> _users;
    private readonly ConcurrentDictionary<string, ChatRoom> _chatRooms;
    private readonly ConcurrentDictionary<string, Message> _messages;
    private bool _isInitialized = false;
    private readonly object _initLock = new();

    public MockDbContext(ILogger<MockDbContext> logger)
    {
        _logger = logger;
        _users = new ConcurrentDictionary<string, User>();
        _chatRooms = new ConcurrentDictionary<string, ChatRoom>();
        _messages = new ConcurrentDictionary<string, Message>();
    }

    /// <summary>
    /// 用户集合（模拟数据库表）
    /// </summary>
    public IEnumerable<User> Users => _users.Values;

    /// <summary>
    /// 聊天室集合（模拟数据库表）
    /// </summary>
    public IEnumerable<ChatRoom> ChatRooms => _chatRooms.Values;

    /// <summary>
    /// 消息集合（模拟数据库表）
    /// </summary>
    public IEnumerable<Message> Messages => _messages.Values;

    /// <summary>
    /// 初始化模拟数据（模拟从数据库加载数据）
    /// </summary>
    public Task InitializeAsync()
    {
        if (_isInitialized)
            return Task.CompletedTask;

        lock (_initLock)
        {
            if (_isInitialized)
                return Task.CompletedTask;

            _logger.LogInformation("开始初始化模拟数据库...");

            try
            {
                // 加载用户数据
                LoadUsers();
                
                // 加载聊天室数据
                LoadChatRooms();
                
                // 加载消息数据
                LoadMessages();

                _isInitialized = true;
                _logger.LogInformation("模拟数据库初始化完成 - 用户: {UserCount}, 聊天室: {RoomCount}, 消息: {MessageCount}",
                    _users.Count, _chatRooms.Count, _messages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化模拟数据库时发生错误");
                throw;
            }
        }
        
        return Task.CompletedTask;
    }

    private void LoadUsers()
    {
        var jsonPath = GetJsonFilePath("users.json");
        var json = File.ReadAllText(jsonPath);
        var users = JsonConvert.DeserializeObject<List<User>>(json) ?? new List<User>();

        foreach (var user in users)
        {
            _users.TryAdd(user.Id, user);
        }

        _logger.LogDebug("加载了 {Count} 个用户", users.Count);
    }

    private void LoadChatRooms()
    {
        var jsonPath = GetJsonFilePath("chatrooms.json");
        var json = File.ReadAllText(jsonPath);
        var chatRooms = JsonConvert.DeserializeObject<List<ChatRoom>>(json) ?? new List<ChatRoom>();

        foreach (var room in chatRooms)
        {
            _chatRooms.TryAdd(room.Id, room);
        }

        _logger.LogDebug("加载了 {Count} 个聊天室", chatRooms.Count);
    }

    private void LoadMessages()
    {
        var jsonPath = GetJsonFilePath("messages.json");
        var json = File.ReadAllText(jsonPath);
        var messages = JsonConvert.DeserializeObject<List<Message>>(json) ?? new List<Message>();

        foreach (var message in messages)
        {
            _messages.TryAdd(message.Id, message);
        }

        _logger.LogDebug("加载了 {Count} 条消息", messages.Count);
    }

    private string GetJsonFilePath(string fileName)
    {
        // 获取当前程序集的位置
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyLocation = Path.GetDirectoryName(assembly.Location) ?? "";
        
        // 构建 MockData 文件夹的路径
        var mockDataPath = Path.Combine(assemblyLocation, "MockData", fileName);
        
        // 如果在程序集位置找不到，尝试在源代码目录中查找
        if (!File.Exists(mockDataPath))
        {
            // 从当前工作目录向上查找
            var currentDir = Directory.GetCurrentDirectory();
            var possiblePaths = new[]
            {
                Path.Combine(currentDir, "src", "OrleansObserverExample.Data", "MockData", fileName),
                Path.Combine(currentDir, "MockData", fileName),
                Path.Combine(currentDir, "..", "OrleansObserverExample.Data", "MockData", fileName)
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    mockDataPath = path;
                    break;
                }
            }
        }

        if (!File.Exists(mockDataPath))
        {
            throw new FileNotFoundException($"找不到模拟数据文件: {fileName}，查找路径: {mockDataPath}");
        }

        return mockDataPath;
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public User? GetUser(string id)
    {
        _users.TryGetValue(id, out var user);
        return user;
    }

    /// <summary>
    /// 根据ID获取聊天室
    /// </summary>
    public ChatRoom? GetChatRoom(string id)
    {
        _chatRooms.TryGetValue(id, out var room);
        return room;
    }

    /// <summary>
    /// 获取聊天室的消息
    /// </summary>
    public IEnumerable<Message> GetMessagesForRoom(string roomId)
    {
        return _messages.Values.Where(m => m.ChatRoomId == roomId && !m.IsDeleted)
                              .OrderBy(m => m.SentAt);
    }

    /// <summary>
    /// 添加用户
    /// </summary>
    public bool AddUser(User user)
    {
        return _users.TryAdd(user.Id, user);
    }

    /// <summary>
    /// 添加消息
    /// </summary>
    public bool AddMessage(Message message)
    {
        return _messages.TryAdd(message.Id, message);
    }

    /// <summary>
    /// 模拟保存更改（在真实数据库中这会执行 SaveChanges）
    /// </summary>
    public Task<int> SaveChangesAsync()
    {
        // 在真实场景中，这里会将更改保存到数据库
        // 现在我们只是记录一下日志
        _logger.LogDebug("模拟保存数据库更改");
        return Task.FromResult(1);
    }

    public void Dispose()
    {
        _logger.LogDebug("释放 MockDbContext 资源");
        // 在真实场景中，这里会释放数据库连接等资源
    }
}

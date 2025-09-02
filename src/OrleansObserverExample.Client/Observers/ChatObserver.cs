using OrleansObserverExample.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace OrleansObserverExample.Client.Observers;

/// <summary>
/// 聊天观察者实现类
/// 客户端实现IChatObserver接口来接收来自服务器的通知
/// </summary>
public class ChatObserver : IChatObserver
{
    private readonly ILogger<ChatObserver> _logger;
    private readonly string _clientName;

    public ChatObserver(ILogger<ChatObserver> logger, string clientName)
    {
        _logger = logger;
        _clientName = clientName;
    }

    /// <summary>
    /// 接收聊天消息
    /// </summary>
    /// <param name="message">消息内容</param>
    public void ReceiveMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logger.LogInformation("[{Timestamp}] {ClientName} 收到消息: {Message}", 
            timestamp, _clientName, message);
        
        // 在控制台显示消息
        Console.WriteLine($"[{timestamp}] 💬 {message}");
    }

    /// <summary>
    /// 接收系统通知
    /// </summary>
    /// <param name="notification">通知内容</param>
    public void ReceiveNotification(string notification)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logger.LogInformation("[{Timestamp}] {ClientName} 收到系统通知: {Notification}", 
            timestamp, _clientName, notification);
        
        // 在控制台显示系统通知
        Console.WriteLine($"[{timestamp}] 🔔 {notification}");
    }
}

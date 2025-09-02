using Orleans;

namespace OrleansObserverExample.Shared.Interfaces;

/// <summary>
/// 聊天观察者接口，继承自IGrainObserver
/// 所有方法都必须是void类型，用于单向异步通知
/// </summary>
public interface IChatObserver : IGrainObserver
{
    /// <summary>
    /// 接收消息的方法
    /// </summary>
    /// <param name="message">消息内容</param>
    void ReceiveMessage(string message);
    
    /// <summary>
    /// 接收系统通知的方法
    /// </summary>
    /// <param name="notification">系统通知</param>
    void ReceiveNotification(string notification);
}

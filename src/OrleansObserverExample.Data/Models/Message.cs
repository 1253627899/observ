namespace OrleansObserverExample.Data.Models;

/// <summary>
/// 消息实体模型
/// </summary>
public class Message
{
    public string Id { get; set; } = string.Empty;
    public string ChatRoomId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTime SentAt { get; set; }
    public bool IsDeleted { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    Text,
    System,
    Notification,
    Image,
    File
}




namespace OrleansObserverExample.Data.Models;

/// <summary>
/// 聊天室实体模型
/// </summary>
public class ChatRoom
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Members { get; set; } = new();
    public bool IsActive { get; set; }
    public int MaxMembers { get; set; } = 100;
    public Dictionary<string, object> Settings { get; set; } = new();
}




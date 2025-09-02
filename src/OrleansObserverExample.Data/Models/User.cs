namespace OrleansObserverExample.Data.Models;

/// <summary>
/// 用户实体模型
/// </summary>
public class User
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = "User";
    public Dictionary<string, object> Metadata { get; set; } = new();
}





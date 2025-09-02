using System;
using System.Collections.Generic;

namespace OrleansObserverExample.Shared.Models;

/// <summary>
/// 玩家信息数据模型
/// </summary>
public class PlayerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime JoinTime { get; set; }
    public List<string> JoinedRooms { get; set; } = new();
    public bool IsOnline { get; set; }
}

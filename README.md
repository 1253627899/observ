# Orleans 观察者模式示例

这是一个完整的Microsoft Orleans观察者模式示例项目，展示了如何使用Orleans框架实现异步通知机制。

## 项目结构

```
OrleansObserverExample/
├── OrleansObserverExample.sln          # 解决方案文件
├── src/
│   ├── OrleansObserverExample.Shared/  # 共享接口和模型
│   │   ├── OrleansObserverExample.Shared.csproj
│   │   ├── IChatObserver.cs            # 观察者接口
│   │   └── IChatGrain.cs               # Grain接口
│   ├── OrleansObserverExample.Server/  # 服务器端实现
│   │   ├── OrleansObserverExample.Server.csproj
│   │   ├── Program.cs                   # 服务器入口
│   │   └── ChatGrain.cs                # Grain实现
│   └── OrleansObserverExample.Client/  # 客户端实现
│       ├── OrleansObserverExample.Client.csproj
│       ├── Program.cs                   # 客户端入口
│       └── ChatObserver.cs             # 观察者实现
└── README.md                           # 项目说明
```

## 核心概念

### 观察者模式
- **IChatObserver**: 继承自`IGrainObserver`的观察者接口
- **单向通信**: 所有方法都是void类型，用于异步通知
- **订阅管理**: 使用`ObserverSubscriptionManager<T>`管理观察者

### 关键组件
1. **IChatObserver**: 定义观察者接口，包含接收消息和通知的方法
2. **IChatGrain**: 定义Grain接口，包含订阅、取消订阅和发送消息的方法
3. **ChatGrain**: 实现Grain逻辑，管理观察者订阅并发送通知
4. **ChatObserver**: 客户端观察者实现，接收来自服务器的通知

## 运行步骤

### 1. 启动服务器
```bash
cd src/OrleansObserverExample.Server
dotnet run
```

### 2. 启动客户端
```bash
cd src/OrleansObserverExample.Client
dotnet run
```

## 工作流程

1. **服务器启动**: 创建Orleans Silo，注册ChatGrain
2. **客户端连接**: 连接到Orleans集群
3. **创建观察者**: 客户端创建ChatObserver实例
4. **订阅通知**: 使用`CreateObjectReference`创建引用并订阅
5. **发送消息**: 服务器向所有订阅的观察者发送消息
6. **接收通知**: 客户端观察者接收并显示消息
7. **取消订阅**: 客户端可以取消订阅停止接收通知

## 重要特性

- **异步通知**: 服务器可以异步向多个客户端发送消息
- **订阅管理**: 支持动态添加和移除观察者
- **错误处理**: 包含基本的错误处理和日志记录
- **生命周期管理**: 正确处理观察者的订阅和取消订阅

## 注意事项

- 观察者本质上是不可靠的，可能丢失消息
- 建议定期轮询或重新订阅来确保消息接收
- 传递给`CreateObjectReference`的对象通过`WeakReference<T>`管理
- 需要维护观察者引用防止被垃圾回收

## 技术栈

- .NET 9.0
- Microsoft Orleans 8.0
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Logging

## 扩展建议

- 添加消息持久化
- 实现消息确认机制
- 添加观察者健康检查
- 支持消息过滤和路由
- 实现观察者组和权限管理

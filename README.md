# A2UI for .NET Blazor

A complete .NET 9 implementation of the A2UI (Agent to UI) protocol for Blazor applications.

## 项目结构

```
blazor-dotnet/
├── src/
│   ├── A2UI.Core/                    # 核心协议处理库
│   ├── A2UI.Blazor.Components/       # Blazor 组件库  
│   ├── A2UI.AgentSDK/                # Agent 端 SDK
│   └── A2UI.Theming/                 # 主题系统
├── samples/
│   ├── A2UI.Sample.BlazorServer/     # Blazor Server 示例
│   ├── A2UI.Sample.BlazorWasm/       # Blazor WASM 示例
│   └── A2UI.Sample.Agent/            # .NET Agent 示例
├── tests/
│   ├── A2UI.Core.Tests/              # 核心库测试
│   └── A2UI.Blazor.Components.Tests/ # 组件测试
└── A2UI.Blazor.sln                   # 解决方案文件
```

## 什么是 A2UI?

A2UI (Agent to UI) 是一个声明式 UI 协议，用于代理驱动的界面。AI 代理生成丰富的交互式 UI，可在各个平台（Web、移动、桌面）上原生渲染，而无需执行任意代码。

### 核心价值

1. **安全性**：声明式数据，而非代码。代理从客户端的可信目录请求组件。无代码执行风险。
2. **原生体验**：无 iframe。客户端使用自己的 UI 框架渲染。继承应用样式、可访问性和性能。
3. **可移植性**：一个代理响应可在任何地方工作。相同的 JSON 可在 Web（Lit/Angular/React/Blazor）、移动（Flutter/SwiftUI）、桌面上渲染。

## 快速开始

### 安装

```bash
# 从解决方案根目录
dotnet restore
dotnet build
```

### 运行示例

```bash
# Blazor Server 示例
cd samples/A2UI.Sample.BlazorServer
dotnet run

# Blazor WASM 示例
cd samples/A2UI.Sample.BlazorWasm
dotnet run
```

访问 `https://localhost:5001/a2ui-demo` 查看交互式演示。

## 使用方式

### 1. 项目引用

在您的 Blazor 项目中添加以下项目引用：

```xml
<ItemGroup>
  <ProjectReference Include="path\to\A2UI.Core\A2UI.Core.csproj" />
  <ProjectReference Include="path\to\A2UI.Blazor.Components\A2UI.Blazor.Components.csproj" />
  <ProjectReference Include="path\to\A2UI.AgentSDK\A2UI.AgentSDK.csproj" />
  <ProjectReference Include="path\to\A2UI.Theming\A2UI.Theming.csproj" />
</ItemGroup>
```

### 2. 配置服务

在 `Program.cs` 中注册 A2UI 服务：

```csharp
using A2UI.Core.Processing;
using A2UI.Theming;

var builder = WebApplication.CreateBuilder(args);

// 添加 Blazor 服务
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 添加 A2UI 服务 (Blazor Server 使用 Scoped)
builder.Services.AddScoped<MessageProcessor>();
builder.Services.AddScoped<DataBindingResolver>(sp => 
    new DataBindingResolver(sp.GetRequiredService<MessageProcessor>()));
builder.Services.AddScoped<EventDispatcher>();
builder.Services.AddSingleton<ThemeService>();

var app = builder.Build();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();
```

> **注意**：Blazor Server 使用 `AddScoped`，Blazor WebAssembly 使用 `AddSingleton`。

### 3. 在页面中使用 A2UI

#### 基础用法 - 显示 Surface

```razor
@page "/demo"
@using A2UI.Core.Processing
@inject MessageProcessor MessageProcessor
@rendermode InteractiveServer

<A2UISurface SurfaceId="my-surface" />

@code {
    protected override void OnInitialized()
    {
        // 创建 A2UI 消息
        var messages = new List<ServerToClientMessage>
        {
            new ServerToClientMessage
            {
                BeginRendering = new BeginRenderingMessage
                {
                    SurfaceId = "my-surface",
                    Root = "root-component"
                }
            },
            new ServerToClientMessage
            {
                SurfaceUpdate = new SurfaceUpdateMessage
                {
                    SurfaceId = "my-surface",
                    Components = new List<ComponentDefinition>
                    {
                        new ComponentDefinition
                        {
                            Id = "root-component",
                            Component = new Dictionary<string, object>
                            {
                                ["Text"] = new Dictionary<string, object>
                                {
                                    ["text"] = new Dictionary<string, object>
                                    {
                                        ["literalString"] = "Hello A2UI!"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // 处理消息
        foreach (var msg in messages)
        {
            MessageProcessor.ProcessMessage(msg);
        }
    }
}
```

#### 使用 Agent SDK 构建 UI

使用 `SurfaceBuilder` 流式 API 快速构建界面：

```csharp
using A2UI.AgentSDK;
using A2UI.AgentSDK.Builders;

// 创建简单的文本卡片
var messages = A2UIQuickStart.CreateTextCard(
    surfaceId: "my-surface",
    title: "欢迎使用 A2UI",
    body: "这是一个使用 Agent SDK 创建的卡片界面"
);

// 创建表单界面
var messages = new SurfaceBuilder("form-surface")
    .AddColumn("root", col => col
        .AddChild("title")
        .AddChild("name-field")
        .AddChild("email-field")
        .AddChild("submit-btn"))
    .AddText("title", text => text
        .WithText("用户注册")
        .WithUsageHint(A2UIConstants.TextUsageHints.H2))
    .AddTextField("name-field", field => field
        .WithPlaceholder("请输入姓名")
        .WithValue("$.name"))
    .AddTextField("email-field", field => field
        .WithPlaceholder("请输入邮箱")
        .WithValue("$.email"))
    .AddButton("submit-btn", btn => btn
        .WithChild("submit-text")
        .WithAction("submit_form")
        .AsPrimary())
    .AddText("submit-text", text => text
        .WithText("提交"))
    .WithRoot("root")
    .Build();

// 处理消息
foreach (var msg in messages)
{
    MessageProcessor.ProcessMessage(msg);
}
```

### 4. 处理用户交互

订阅用户操作事件：

```csharp
@inject EventDispatcher EventDispatcher

@code {
    protected override void OnInitialized()
    {
        // 订阅用户操作
        EventDispatcher.UserActionDispatched += OnUserAction;
    }

    private async void OnUserAction(object? sender, UserActionEventArgs e)
    {
        var action = e.Action;
        Console.WriteLine($"用户操作: {action.Name}");
        Console.WriteLine($"来源组件: {action.SourceComponentId}");
        Console.WriteLine($"Surface ID: {action.SurfaceId}");
        
        // 获取上下文数据
        if (action.Context != null)
        {
            foreach (var (key, value) in action.Context)
            {
                Console.WriteLine($"  {key}: {value}");
            }
        }

        // 根据操作更新 UI
        if (action.Name == "submit_form")
        {
            var name = action.Context?["name"];
            var email = action.Context?["email"];
            // 处理表单提交...
            
            // 更新 UI 显示结果
            var resultMessages = A2UIQuickStart.CreateTextCard(
                action.SurfaceId,
                "提交成功",
                $"欢迎 {name}！"
            );
            
            foreach (var msg in resultMessages)
            {
                MessageProcessor.ProcessMessage(msg);
            }
            
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        EventDispatcher.UserActionDispatched -= OnUserAction;
    }
}
```

### 5. 创建 Agent 服务

创建一个 Agent 服务来处理用户查询：

```csharp
public class MyAgent
{
    public Task<List<ServerToClientMessage>> ProcessQueryAsync(string query)
    {
        // 根据查询生成相应的 UI
        if (query.Contains("天气"))
        {
            return Task.FromResult(CreateWeatherUI());
        }
        else if (query.Contains("列表"))
        {
            return Task.FromResult(CreateListUI());
        }
        
        return Task.FromResult(CreateDefaultUI());
    }

    private List<ServerToClientMessage> CreateWeatherUI()
    {
        return new SurfaceBuilder("weather-surface")
            .AddCard("card", card => card
                .WithChild("content"))
            .AddColumn("content", col => col
                .AddChild("icon")
                .AddChild("temp")
                .AddChild("desc"))
            .AddIcon("icon", icon => icon
                .WithIcon("☀️"))
            .AddText("temp", text => text
                .WithText("25°C")
                .WithUsageHint(A2UIConstants.TextUsageHints.H1))
            .AddText("desc", text => text
                .WithText("晴朗"))
            .WithRoot("card")
            .Build();
    }
}
```

### 6. 完整的聊天界面示例

参考 `samples/A2UI.Sample.BlazorServer/Components/Pages/A2UIDemo.razor` 查看完整的聊天界面实现，包括：

- 💬 消息历史记录
- ⚡ 动态 UI 生成
- 🎯 用户操作处理
- 🔄 实时状态更新

### 7. 支持的组件

A2UI 支持以下标准组件：

| 组件 | 说明 | 示例用途 |
|------|------|----------|
| `Text` | 文本显示 | 标题、段落、标签 |
| `Button` | 按钮 | 提交、取消、操作 |
| `Card` | 卡片容器 | 内容分组 |
| `Row` / `Column` | 布局容器 | 横向/纵向布局 |
| `TextField` | 文本输入框 | 表单输入 |
| `CheckBox` | 复选框 | 选项选择 |
| `DateTimeInput` | 日期时间选择器 | 日期选择 |
| `Slider` | 滑块 | 数值调整 |
| `MultipleChoice` | 单选/多选 | 选项列表 |
| `List` | 列表 | 数据展示 |
| `Tabs` | 选项卡 | 内容切换 |
| `Modal` | 模态框 | 弹窗、确认 |
| `Image` | 图片 | 图片显示 |
| `Icon` | 图标 | 装饰、状态 |
| `Video` | 视频播放器 | 视频内容 |
| `AudioPlayer` | 音频播放器 | 音频内容 |
| `Divider` | 分割线 | 内容分隔 |

### 8. 数据绑定

使用 `BoundValue` 进行数据绑定：

```csharp
// 设置数据
var dataMessages = new List<ServerToClientMessage>
{
    new ServerToClientMessage
    {
        DataModelUpdate = new DataModelUpdateMessage
        {
            SurfaceId = "my-surface",
            Path = "/",
            Contents = new List<DataEntry>
            {
                new DataEntry { Key = "username", ValueString = "张三" },
                new DataEntry { Key = "age", ValueNumber = 25 },
                new DataEntry { Key = "isVip", ValueBoolean = true }
            }
        }
    }
};

// 在组件中使用数据绑定
.AddText("name-display", text => text
    .WithValue("$.username"))  // 绑定到数据模型
```

## NuGet 包

- `A2UI.Core` - 核心协议库
- `A2UI.Blazor` - Blazor 组件库
- `A2UI.AgentSDK` - Agent 开发 SDK
- `A2UI.Theming` - 主题系统

## 技术栈

- .NET 9.0
- Blazor Server（通过 SignalR）
- Blazor WebAssembly（通过 HttpClient + SSE）
- 符合 A2UI 0.8 协议规范

## 文档

- [A2UI 协议规范](../specification/0.8/docs/a2ui_protocol.md)
- [组件目录](../specification/0.8/json/standard_catalog_definition.json)
- [渲染器开发指南](../docs/guides/renderer-development.md)

## 许可证

Apache 2.0 - 参见 [LICENSE](../LICENSE)

## 贡献

欢迎贡献！请参阅 [CONTRIBUTING.md](../CONTRIBUTING.md) 了解详情。


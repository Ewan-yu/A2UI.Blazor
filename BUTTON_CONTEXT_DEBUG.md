# 按钮点击时 ResolveActionContext 未执行的问题分析

## 问题描述

当点击按钮时，`BindingResolver.ResolveActionContext` 方法没有被执行。

## 原因分析

查看 `A2UIButton.razor` 中的代码逻辑：

```razor
private void HandleClick()
{
    // ... 检查 ActionData ...
    
    // Resolve context
    Dictionary<string, object> context = new();
    if (ActionData.TryGetValue("context", out var contextObj) && 
        contextObj is List<Dictionary<string, object>> contextEntries)
    {
        // 只有当 ActionData 包含 "context" 字段时才会执行
        context = BindingResolver.ResolveActionContext(contextEntries, SurfaceId, Component.DataContextPath);
    }
}
```

**关键点**：只有当按钮的 `action` 配置中包含 `context` 字段时，`ResolveActionContext` 才会被调用。

## 检查 Mock 数据

查看 `samples/A2UI.Sample.BlazorServer/MockData/buttons.json`：

```json
{
  "id": "btn1",
  "component": {
    "Button": {
      "child": "btn1-text",
      "primary": true,
      "action": { 
        "name": "like_action"
        // ❌ 没有 context 字段！
      }
    }
  }
}
```

**结论**：原来的 `buttons.json` 中的按钮配置**没有** `context` 字段，所以 `ResolveActionContext` 不会被执行。这是**正常行为**！

## 解决方案

### 1. 带 Context 的按钮示例

创建了新文件 `buttons_with_context.json`，包含两种 context 使用方式：

#### 方式一：从数据模型绑定（path）

```json
{
  "id": "btn1",
  "component": {
    "Button": {
      "child": "btn1-text",
      "primary": true,
      "action": {
        "name": "delete_item",
        "context": [
          {
            "key": "itemId",
            "value": {
              "path": "itemId"  // ✅ 从数据模型读取
            }
          },
          {
            "key": "user",
            "value": {
              "path": "userName"
            }
          }
        ]
      }
    }
  }
}
```

#### 方式二：使用字面量值（literalString）

```json
{
  "id": "btn2",
  "component": {
    "Button": {
      "child": "btn2-text",
      "action": {
        "name": "share_item",
        "context": [
          {
            "key": "itemId",
            "value": {
              "literalString": "hardcoded-item-456"  // ✅ 硬编码值
            }
          },
          {
            "key": "shareType",
            "value": {
              "literalString": "public"
            }
          }
        ]
      }
    }
  }
}
```

### 2. 测试方法

在 Demo 页面中点击 **"🔘 带上下文按钮"** 按钮，会加载 `buttons_with_context.json`。

然后点击生成的按钮，在浏览器控制台会看到：

```
[A2UIButton] HandleClick: ComponentId=btn1
[A2UIButton] Action name: delete_item
[A2UIButton] ActionData keys: name, context
[A2UIButton] Context object type: List`1
[A2UIButton] Context entries count: 2
[A2UIButton] Resolved context: {"itemId":"item-123","user":"张三"}
[A2UIButton] Dispatching user action: delete_item
```

**✅ 可以看到 `ResolveActionContext` 被成功执行了！**

## 调试日志说明

为了方便调试，在 `A2UIButton.razor` 中添加了详细的日志：

```csharp
Console.WriteLine($"[A2UIButton] ActionData keys: {string.Join(", ", ActionData.Keys)}");

if (ActionData.TryGetValue("context", out var contextObj))
{
    Console.WriteLine($"[A2UIButton] Context object type: {contextObj?.GetType().Name ?? "null"}");
    Console.WriteLine($"[A2UIButton] Context object value: {System.Text.Json.JsonSerializer.Serialize(contextObj)}");
    
    if (contextObj is List<Dictionary<string, object>> contextEntries)
    {
        Console.WriteLine($"[A2UIButton] Context entries count: {contextEntries.Count}");
        context = BindingResolver.ResolveActionContext(contextEntries, SurfaceId, Component.DataContextPath);
        Console.WriteLine($"[A2UIButton] Resolved context: {System.Text.Json.JsonSerializer.Serialize(context)}");
    }
    else
    {
        Console.WriteLine($"[A2UIButton] Context is not List<Dictionary<string, object>>");
    }
}
else
{
    Console.WriteLine($"[A2UIButton] No context in ActionData");
}
```

这些日志会帮助你：
- 查看 ActionData 中包含哪些字段
- 确认 context 字段是否存在
- 查看 context 的类型和值
- 查看解析后的 context 内容

## Context 字段的结构

根据 A2UI 协议，action context 的结构是：

```json
"context": [
  {
    "key": "参数名",
    "value": {
      "path": "数据模型路径"           // 方式1: 从数据模型绑定
      // 或
      "literalString": "字符串值"      // 方式2: 字面量字符串
      // 或
      "literalNumber": 123              // 方式3: 字面量数字
      // 或
      "literalBoolean": true            // 方式4: 字面量布尔值
    }
  }
]
```

## 总结

1. **原始问题**：按钮没有配置 `context` 字段
2. **解决方案**：在按钮的 action 中添加 context 配置
3. **测试文件**：`buttons_with_context.json`
4. **测试方法**：点击 "带上下文按钮" 快捷按钮
5. **验证**：查看浏览器控制台的调试日志

## 相关文件

- `src/A2UI.Blazor.Components/Components/A2UIButton.razor` - 按钮组件
- `src/A2UI.Core/Processing/DataBindingResolver.cs` - Context 解析逻辑
- `samples/A2UI.Sample.BlazorServer/MockData/buttons.json` - 原始按钮（无 context）
- `samples/A2UI.Sample.BlazorServer/MockData/buttons_with_context.json` - 带 context 的按钮
- `samples/A2UI.Sample.BlazorServer/Services/MockA2AAgent.cs` - Mock Agent 配置


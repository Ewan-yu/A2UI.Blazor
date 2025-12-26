# Button Context 传递问题修复

## 问题描述

Button 组件的 `action.context` 无法正确解析，导致 `BindingResolver.ResolveActionContext` 没有被调用。

## 问题根源

### 1. A2UI 规范版本差异

项目使用的是 **A2UI v0.8 规范**，其中 Button action 的 context 格式为：

```json
{
  "action": {
    "name": "action_name",
    "context": [
      { "key": "param1", "value": { "literalString": "value1" } },
      { "key": "param2", "value": { "path": "/data/path" } }
    ]
  }
}
```

**注意**：`context` 是一个**数组**格式（v0.8），不是对象格式（v0.9）。

### 2. JSON 反序列化类型不匹配

在 `MessageProcessor.ParseComponentDefinition` 中：

```csharp
var properties = def.Component[componentType] as Dictionary<string, object> 
    ?? new Dictionary<string, object>();
```

当 JSON 反序列化时：
- `action` 属性被反序列化为 `Dictionary<string, object>`
- `context` 数组被反序列化为 **`JsonElement`** 类型（而不是 `List<Dictionary<string, object>>`）

这导致在 `A2UIButton.razor` 中类型检查失败：

```csharp
// ❌ 这个检查永远不会成功，因为 contextObj 是 JsonElement
if (contextObj is List<Dictionary<string, object>> contextEntries)
{
    context = BindingResolver.ResolveActionContext(contextEntries, ...);
}
```

## 解决方案

### 修改 1: A2UIButton.razor

添加对 `JsonElement` 的处理：

```csharp
// Handle JsonElement (from JSON deserialization)
if (contextObj is JsonElement jsonElement)
{
    if (jsonElement.ValueKind == JsonValueKind.Array)
    {
        contextEntries = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
            jsonElement.GetRawText()
        );
    }
}
// Handle direct List (unlikely but possible)
else if (contextObj is List<Dictionary<string, object>> directList)
{
    contextEntries = directList;
}
```

### 修改 2: DataBindingResolver.cs

在 `ResolveActionContext` 方法中也添加对 `JsonElement` 的处理：

```csharp
// Handle JsonElement (from JSON deserialization)
Dictionary<string, object>? valueDict = null;

if (valueObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
{
    valueDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
        jsonElement.GetRawText()
    );
}
else if (valueObj is Dictionary<string, object> dict)
{
    valueDict = dict;
}
```

### 修改 3: 添加调试日志

添加了详细的调试日志来追踪 context 的解析过程：

- 显示 ActionData 的所有键
- 显示 contextObj 的实际类型
- 显示 JsonElement 的 ValueKind
- 显示解析成功或失败的信息
- 显示最终 resolved context 的键值

## 测试数据

创建了 `buttons-with-context.json` 测试文件，包含三种情况：

1. **带静态 context**: 使用 `literalString` 传递固定值
2. **带动态 context**: 使用 `path` 从数据模型读取值
3. **无 context**: 不带任何 context 参数

```json
{
  "id": "btn1",
  "component": {
    "Button": {
      "child": "btn1-text",
      "primary": true,
      "action": { 
        "name": "like_action",
        "context": [
          { "key": "itemId", "value": { "literalString": "item-123" } },
          { "key": "userId", "value": { "literalString": "user-456" } }
        ]
      }
    }
  }
}
```

## 验证步骤

1. 启动应用程序
2. 在演示页面选择 "带 Context 的按钮" 场景
3. 打开浏览器控制台
4. 点击按钮
5. 查看控制台输出，应该看到：
   ```
   [A2UIButton] ActionData keys: name, context
   [A2UIButton] Context found, type: JsonElement
   [A2UIButton] Context is JsonElement, ValueKind: Array
   [A2UIButton] ✓ Deserialized JsonElement array, count: 2
   [DataBindingResolver] ✓ Resolved context 'itemId' = item-123
   [DataBindingResolver] ✓ Resolved context 'userId' = user-456
   [A2UIButton] ✓ Resolved context keys: itemId, userId
   ```

## 关键要点

1. **规范版本很重要**：v0.8 使用数组，v0.9 使用对象
2. **JSON 反序列化陷阱**：`Dictionary<string, object>` 中的复杂类型会变成 `JsonElement`
3. **类型检查必须处理 JsonElement**：不能只检查强类型
4. **调试日志很有价值**：帮助快速定位问题

## 相关文件

- `src/A2UI.Blazor.Components/Components/A2UIButton.razor` - Button 组件
- `src/A2UI.Core/Processing/DataBindingResolver.cs` - 数据绑定解析器
- `samples/A2UI.Sample.BlazorServer/MockData/buttons-with-context.json` - 测试数据
- `A2UI/specification/0.8/json/standard_catalog_definition.json` - v0.8 规范
- `A2UI/specification/0.9/json/standard_catalog_definition.json` - v0.9 规范

## 未来改进建议

1. 考虑创建一个统一的 JSON 反序列化辅助类来处理 `JsonElement`
2. 为所有组件添加类似的 `JsonElement` 处理逻辑
3. 考虑升级到 v0.9 规范（context 使用对象格式，更简洁）
4. 添加单元测试来验证 context 解析逻辑


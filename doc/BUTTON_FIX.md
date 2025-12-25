# 按钮显示问题修复

## 问题描述
当用户点击"显示按钮"快捷按钮时，Agent 返回的 Button 组件无法正确显示。

## 根本原因
问题出在组件属性的类型处理上。当 A2UI 消息从 `MockA2AAgent` 返回后，某些属性（特别是 `Dictionary<string, object>` 和数组）在内存中被正确构造，但这些对象的嵌套属性可能是不同的运行时类型（如 `string[]`、`object[]` 或 `System.Text.Json.JsonElement`）。

Blazor 组件在解析这些属性时，原有的类型检查不够全面，导致：
1. `A2UIRow` 和 `A2UIColumn` 无法正确解析 `explicitList` 数组
2. `A2UIButton` 无法正确获取 `action` 字典属性
3. 最终导致按钮组件树无法正确构建

## 修复内容

### 1. 增强 `A2UIComponentBase.GetDictionaryProperty()` 方法
**文件**: `src/A2UI.Blazor.Components/A2UIComponentBase.cs`

添加了对 `JsonElement` 类型的特殊处理，确保可以正确反序列化嵌套的字典对象：

```csharp
protected Dictionary<string, object>? GetDictionaryProperty(string propertyName)
{
    if (Component.Properties.TryGetValue(propertyName, out var value))
    {
        // Direct dictionary
        if (value is Dictionary<string, object> dict)
            return dict;

        // JsonElement needs special handling
        if (value is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[A2UIComponentBase] Failed to deserialize JsonElement to Dictionary for property '{propertyName}': {ex.Message}");
                return null;
            }
        }
    }
    return null;
}
```

### 2. 增强 `A2UIRow` 组件的数组解析
**文件**: `src/A2UI.Blazor.Components/Components/A2UIRow.razor`

添加了对多种数组类型的支持：
- `string[]` - 字符串数组
- `object[]` - 对象数组
- `System.Text.Json.JsonElement` - JSON 元素（需要反序列化）

并添加了详细的调试日志输出。

### 3. 增强 `A2UIColumn` 组件的数组解析
**文件**: `src/A2UI.Blazor.Components/Components/A2UIColumn.razor`

与 `A2UIRow` 相同的修复，确保 Column 布局组件也能正确解析子组件列表。

### 4. 增强 `A2UIList` 组件的数组解析
**文件**: `src/A2UI.Blazor.Components/Components/A2UIList.razor`

与其他布局组件相同的修复。

## 测试步骤

1. 启动应用：
   ```bash
   cd samples/A2UI.Sample.BlazorServer
   dotnet run
   ```

2. 在浏览器中打开 `http://localhost:5000` 或 `https://localhost:5001`

3. 点击页面上的 "🔘 显示按钮" 快捷按钮

4. 预期结果：
   - 应该显示一个卡片
   - 卡片标题："交互按钮演示"
   - 卡片描述："点击按钮与 Agent 交互："
   - 显示两个按钮：
     - "👍 喜欢" (主按钮，蓝色)
     - "🔗 分享" (次级按钮)

5. 点击按钮后，应该触发用户操作事件，并在输入框下方显示操作反馈

## 相关文件
- `src/A2UI.Blazor.Components/A2UIComponentBase.cs`
- `src/A2UI.Blazor.Components/Components/A2UIRow.razor`
- `src/A2UI.Blazor.Components/Components/A2UIColumn.razor`
- `src/A2UI.Blazor.Components/Components/A2UIList.razor`
- `samples/A2UI.Sample.BlazorServer/Services/MockA2AAgent.cs` (第219-370行的 `GetButtonExample()` 方法)

## 技术要点
1. **类型灵活性**: .NET 的 `Dictionary<string, object>` 中的 `object` 值可以是各种运行时类型
2. **JSON 序列化**: 某些情况下属性值可能是 `JsonElement`，需要显式反序列化
3. **数组类型**: 数组可能是 `T[]`、`List<T>` 或 `List<object>`，需要统一处理
4. **调试友好**: 添加详细的 Console.WriteLine 输出，方便排查问题

## 未来改进建议
1. 考虑创建一个统一的类型转换辅助类
2. 在 MessageProcessor 阶段就进行类型规范化
3. 添加单元测试验证各种类型的属性解析


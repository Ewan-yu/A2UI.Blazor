using A2UI.Core.Types;
using System.Text.Json;

namespace A2UI.Core.Processing;

/// <summary>
/// Handles resolution of bound values against the data model.
/// </summary>
public class DataBindingResolver
{
    private readonly MessageProcessor _messageProcessor;

    public DataBindingResolver(MessageProcessor messageProcessor)
    {
        _messageProcessor = messageProcessor;
    }

    /// <summary>
    /// Resolves a bound value from a dictionary representation.
    /// </summary>
    public T? ResolveBoundValue<T>(Dictionary<string, object> boundValue, string surfaceId, string? dataContextPath = null)
    {
        Console.WriteLine($"[DataBindingResolver.ResolveBoundValue] surfaceId={surfaceId}, dataContextPath={dataContextPath}");
        Console.WriteLine($"[DataBindingResolver.ResolveBoundValue] boundValue keys: {string.Join(", ", boundValue.Keys)}");

        // Check for literal values first
        if (boundValue.TryGetValue("literalString", out var literalString) && literalString != null)
        {
            Console.WriteLine($"[DataBindingResolver.ResolveBoundValue] Found literalString: {literalString}");
            if (typeof(T) == typeof(string))
                return (T)literalString;
        }

        if (boundValue.TryGetValue("literalNumber", out var literalNumber) && literalNumber != null)
        {
            Console.WriteLine($"[DataBindingResolver.ResolveBoundValue] Found literalNumber: {literalNumber}");
            if (typeof(T) == typeof(double))
                return (T)Convert.ChangeType(literalNumber, typeof(T));
            if (typeof(T) == typeof(int))
                return (T)Convert.ChangeType(literalNumber, typeof(T));
        }

        if (boundValue.TryGetValue("literalBoolean", out var literalBoolean) && literalBoolean != null)
        {
            Console.WriteLine($"[DataBindingResolver.ResolveBoundValue] Found literalBoolean: {literalBoolean}");
            if (typeof(T) == typeof(bool))
                return (T)literalBoolean;
        }

        // Check for path binding
        if (boundValue.TryGetValue("path", out var pathObj))
        {
            string? path = null;

            // Handle JsonElement
            if (pathObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            {
                path = jsonElement.GetString();
                Console.WriteLine($"[DataBindingResolver.ResolveBoundValue] Found path (from JsonElement): '{path}'");
            }
            else if (pathObj is string pathString)
            {
                path = pathString;
                Console.WriteLine($"[DataBindingResolver.ResolveBoundValue] Found path (direct string): '{path}'");
            }
            else
            {
                Console.WriteLine($"[DataBindingResolver.ResolveBoundValue] ❌ path is not string or JsonElement, type: {pathObj?.GetType().Name ?? "null"}");
            }

            if (!string.IsNullOrEmpty(path))
            {
                var dataValue = _messageProcessor.GetData(surfaceId, path, dataContextPath);
                Console.WriteLine($"[DataBindingResolver.ResolveBoundValue] GetData returned: {dataValue?.ToString() ?? "null"} (type: {dataValue?.GetType().Name ?? "null"})");

                // If both literal and path are present, set the literal value first (initialization shorthand)
                if (boundValue.ContainsKey("literalString") ||
                    boundValue.ContainsKey("literalNumber") ||
                    boundValue.ContainsKey("literalBoolean"))
                {
                    if (boundValue.TryGetValue("literalString", out var ls) && ls != null)
                        _messageProcessor.SetData(surfaceId, path, ls, dataContextPath);
                    else if (boundValue.TryGetValue("literalNumber", out var ln) && ln != null)
                        _messageProcessor.SetData(surfaceId, path, ln, dataContextPath);
                    else if (boundValue.TryGetValue("literalBoolean", out var lb) && lb != null)
                        _messageProcessor.SetData(surfaceId, path, lb, dataContextPath);

                    // Re-get the value after setting
                    dataValue = _messageProcessor.GetData(surfaceId, path, dataContextPath);
                }

                // Convert the data value to the target type
                if (dataValue != null)
                {
                    try
                    {
                        if (dataValue is T typedValue)
                            return typedValue;

                        if (dataValue is JsonElement jsonData)
                        {
                            return jsonData.Deserialize<T>();
                        }

                        return (T)Convert.ChangeType(dataValue, typeof(T));
                    }
                    catch
                    {
                        // Return default if conversion fails
                        return default;
                    }
                }
            }
        }

        return default;
    }

    /// <summary>
    /// Resolves a string bound value.
    /// </summary>
    public string? ResolveString(Dictionary<string, object> boundValue, string surfaceId, string? dataContextPath = null)
    {
        return ResolveBoundValue<string>(boundValue, surfaceId, dataContextPath);
    }

    /// <summary>
    /// Resolves a number bound value.
    /// </summary>
    public double? ResolveNumber(Dictionary<string, object> boundValue, string surfaceId, string? dataContextPath = null)
    {
        return ResolveBoundValue<double>(boundValue, surfaceId, dataContextPath);
    }

    /// <summary>
    /// Resolves a boolean bound value.
    /// </summary>
    public bool? ResolveBool(Dictionary<string, object> boundValue, string surfaceId, string? dataContextPath = null)
    {
        return ResolveBoundValue<bool>(boundValue, surfaceId, dataContextPath);
    }

    /// <summary>
    /// Resolves an array/list bound value.
    /// </summary>
    public List<T>? ResolveList<T>(Dictionary<string, object> boundValue, string surfaceId, string? dataContextPath = null)
    {
        return ResolveBoundValue<List<T>>(boundValue, surfaceId, dataContextPath);
    }

    /// <summary>
    /// Checks if a bound value has a path binding.
    /// </summary>
    public bool HasPathBinding(Dictionary<string, object> boundValue)
    {
        return boundValue.ContainsKey("path") && boundValue["path"] is string path && !string.IsNullOrEmpty(path);
    }

    /// <summary>
    /// Gets the path from a bound value if it exists.
    /// </summary>
    public string? GetPath(Dictionary<string, object> boundValue)
    {
        if (boundValue.TryGetValue("path", out var pathObj) && pathObj is string path)
        {
            return path;
        }
        return null;
    }

    /// <summary>
    /// Resolves a bound value (generic object return).
    /// </summary>
    public object? ResolveValue(Dictionary<string, object> boundValue, string surfaceId, string? dataContextPath = null)
    {
        return ResolveBoundValue<object>(boundValue, surfaceId, dataContextPath);
    }

    /// <summary>
    /// Resolves action context entries against the data model.
    /// </summary>
    public Dictionary<string, object> ResolveActionContext(
        List<Dictionary<string, object>>? contextEntries,
        string surfaceId,
        string? dataContextPath = null)
    {
        var result = new Dictionary<string, object>();

        if (contextEntries == null)
            return result;

        Console.WriteLine($"[DataBindingResolver] ResolveActionContext: processing {contextEntries.Count} entries");

        foreach (var entry in contextEntries)
        {
            Console.WriteLine($"[DataBindingResolver] Entry keys: {string.Join(", ", entry.Keys)}");
            Console.WriteLine($"[DataBindingResolver] Entry JSON: {JsonSerializer.Serialize(entry)}");
            
            if (!entry.TryGetValue("key", out var keyObj))
            {
                Console.WriteLine($"[DataBindingResolver] ❌ Entry missing 'key' property");
                continue;
            }
            
            Console.WriteLine($"[DataBindingResolver] keyObj type: {keyObj?.GetType().FullName ?? "null"}, value: {keyObj}");
            
            if (keyObj is not string key)
            {
                Console.WriteLine($"[DataBindingResolver] ❌ Key is not string, it's {keyObj?.GetType().Name ?? "null"}");
                continue;
            }

            if (!entry.TryGetValue("value", out var valueObj))
            {
                Console.WriteLine($"[DataBindingResolver] ❌ Context entry '{key}' missing 'value'");
                continue;
            }

            Console.WriteLine($"[DataBindingResolver] valueObj type: {valueObj?.GetType().FullName ?? "null"}");

            // Handle JsonElement (from JSON deserialization)
            Dictionary<string, object>? valueDict = null;
            
            if (valueObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                try
                {
                    valueDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                    Console.WriteLine($"[DataBindingResolver] ✓ Deserialized JsonElement to Dictionary");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataBindingResolver] ❌ Failed to deserialize JsonElement for key '{key}': {ex.Message}");
                    continue;
                }
            }
            else if (valueObj is Dictionary<string, object> dict)
            {
                valueDict = dict;
                Console.WriteLine($"[DataBindingResolver] ✓ Value is already Dictionary");
            }
            else
            {
                Console.WriteLine($"[DataBindingResolver] ❌ Context entry '{key}' value is not a Dictionary or JsonElement object, type: {valueObj?.GetType().Name ?? "null"}");
                continue;
            }

            if (valueDict == null)
                continue;

            Console.WriteLine($"[DataBindingResolver] valueDict contents: {JsonSerializer.Serialize(valueDict)}");

            // Resolve the bound value
            var resolvedValue = ResolveBoundValue<object>(valueDict, surfaceId, dataContextPath);
            Console.WriteLine($"[DataBindingResolver] ResolveBoundValue returned: {resolvedValue?.ToString() ?? "null"} (type: {resolvedValue?.GetType().Name ?? "null"})");
            if (resolvedValue != null)
            {
                result[key] = resolvedValue;
                Console.WriteLine($"[DataBindingResolver] ✓ Resolved context '{key}' = {resolvedValue}");
            }
            else
            {
                Console.WriteLine($"[DataBindingResolver] ⚠ Context entry '{key}' resolved to null");
            }
        }

        return result;
    }
}


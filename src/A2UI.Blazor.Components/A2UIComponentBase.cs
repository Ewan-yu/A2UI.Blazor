using A2UI.Core.Types;
using A2UI.Theming;
using Microsoft.AspNetCore.Components;

namespace A2UI.Blazor.Components;

/// <summary>
/// Base class for all A2UI components.
/// </summary>
public abstract class A2UIComponentBase : ComponentBase
{
    /// <summary>
    /// The surface ID this component belongs to.
    /// </summary>
    [Parameter]
    public required string SurfaceId { get; set; }

    /// <summary>
    /// The component node containing all component data.
    /// </summary>
    [Parameter]
    public required ComponentNode Component { get; set; }

    /// <summary>
    /// The theme service.
    /// </summary>
    [Inject]
    protected ThemeService ThemeService { get; set; } = null!;

    /// <summary>
    /// Gets the current theme.
    /// </summary>
    protected IA2UITheme Theme => ThemeService.CurrentTheme;

    /// <summary>
    /// Gets a property value from the component.
    /// </summary>
    protected T? GetProperty<T>(string propertyName)
    {
        if (Component.Properties.TryGetValue(propertyName, out var value))
        {
            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    /// <summary>
    /// Gets a dictionary property.
    /// </summary>
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

    /// <summary>
    /// Gets a string property.
    /// </summary>
    protected string? GetStringProperty(string propertyName)
    {
        return GetProperty<string>(propertyName);
    }

    /// <summary>
    /// Gets a boolean property.
    /// </summary>
    protected bool GetBoolProperty(string propertyName, bool defaultValue = false)
    {
        return GetProperty<bool?>(propertyName) ?? defaultValue;
    }

    /// <summary>
    /// Gets the CSS class for this component.
    /// </summary>
    protected virtual string GetCssClass()
    {
        return "";
    }

    /// <summary>
    /// Gets inline styles for this component.
    /// </summary>
    protected virtual string GetInlineStyle()
    {
        var styles = new List<string>();

        if (Component.Weight.HasValue)
        {
            styles.Add($"flex: {Component.Weight.Value} 1 0%");
        }

        return styles.Count > 0 ? string.Join("; ", styles) : "";
    }
}


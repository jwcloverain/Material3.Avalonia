using Avalonia;
using Avalonia.Controls;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>Provides Material error presentation for native checkboxes.</summary>
public static class CheckBoxAssist
{
    /// <summary>Identifies the manual error state.</summary>
    public static readonly AttachedProperty<bool> IsErrorProperty =
        AvaloniaProperty.RegisterAttached<CheckBox, bool>("IsError", typeof(CheckBoxAssist));

    /// <summary>Gets whether the checkbox has a manual error.</summary>
    public static bool GetIsError(CheckBox checkBox) => checkBox.GetValue(IsErrorProperty);

    /// <summary>Sets whether the checkbox has a manual error.</summary>
    public static void SetIsError(CheckBox checkBox, bool value) => checkBox.SetValue(IsErrorProperty, value);
}
using System.Globalization;

namespace Sovereigns.Client.Controls;

public enum BindingKind
{
    Key,
    Mouse,
}

[Flags]
public enum BindingModifiers
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    Meta = 8,
}

/// <summary>
/// One physical input in the text form stored in the bindings file: <c>key:A</c>, <c>key:Ctrl+Q</c>,
/// <c>mouse:left</c>, <c>mouse:wheel_up</c>. Key names are Godot's <see cref="Key"/> member names.
/// </summary>
public readonly record struct Binding(BindingKind Kind, Key Key, MouseButton Button, BindingModifiers Modifiers)
{
    private static readonly Dictionary<string, MouseButton> MouseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["left"] = MouseButton.Left,
        ["right"] = MouseButton.Right,
        ["middle"] = MouseButton.Middle,
        ["wheel_up"] = MouseButton.WheelUp,
        ["wheel_down"] = MouseButton.WheelDown,
        ["xbutton1"] = MouseButton.Xbutton1,
        ["xbutton2"] = MouseButton.Xbutton2,
    };

    private static readonly Dictionary<string, BindingModifiers> ModifierNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ctrl"] = BindingModifiers.Ctrl,
        ["control"] = BindingModifiers.Ctrl,
        ["shift"] = BindingModifiers.Shift,
        ["alt"] = BindingModifiers.Alt,
        ["meta"] = BindingModifiers.Meta,
        ["cmd"] = BindingModifiers.Meta,
        ["command"] = BindingModifiers.Meta,
    };

    private static readonly Dictionary<Key, string> KeyDisplay = new()
    {
        [Key.Bracketleft] = "[",
        [Key.Bracketright] = "]",
        [Key.Equal] = "=",
        [Key.Minus] = "-",
    };

    public static bool TryParse(string? text, out Binding binding, out string error)
    {
        binding = default;
        var colon = text?.IndexOf(':', StringComparison.Ordinal) ?? -1;
        if (text is null || colon <= 0 || colon == text.Length - 1)
        {
            error = $"'{text}' is not <kind>:<input>, for example key:A, key:Ctrl+Q or mouse:left";
            return false;
        }

        var kindText = text[..colon];
        var parts = text[(colon + 1)..].Split('+');
        var modifiers = BindingModifiers.None;
        foreach (var part in parts[..^1])
        {
            if (!ModifierNames.TryGetValue(part, out var modifier))
            {
                error = $"'{text}' has unknown modifier '{part}'; use Ctrl, Shift, Alt or Meta";
                return false;
            }

            modifiers |= modifier;
        }

        var name = parts[^1];
        if (kindText.Equals("key", StringComparison.OrdinalIgnoreCase))
        {
            // Enum.TryParse accepts numbers; only member names are valid key names.
            if (name.Length == 0 || char.IsDigit(name[0]) || !Enum.TryParse<Key>(name, ignoreCase: true, out var key) ||
                !Enum.IsDefined(key) || key is Key.None or Key.Unknown)
            {
                error = $"'{text}' names no key '{name}'; use a Godot key name such as A, Left, Home, F3 or Bracketleft";
                return false;
            }

            binding = new Binding(BindingKind.Key, key, MouseButton.None, modifiers);
        }
        else if (kindText.Equals("mouse", StringComparison.OrdinalIgnoreCase))
        {
            if (!MouseNames.TryGetValue(name, out var button))
            {
                error = $"'{text}' names no mouse input '{name}'; use {string.Join(", ", MouseNames.Keys)}";
                return false;
            }

            binding = new Binding(BindingKind.Mouse, Key.None, button, modifiers);
        }
        else
        {
            error = $"'{text}' has unknown kind '{kindText}'; use key or mouse";
            return false;
        }

        error = "";
        return true;
    }

    /// <summary>Canonical text form; two bindings conflict exactly when these strings are equal.</summary>
    public override string ToString()
    {
        var button = Button;
        return (Kind == BindingKind.Key ? "key:" : "mouse:") + ModifierPrefix(Modifiers) +
               (Kind == BindingKind.Key ? Key.ToString() : MouseNames.First(pair => pair.Value == button).Key);
    }

    /// <summary>Short text for the help line, for example <c>Ctrl+Q</c>, <c>MMB</c> or <c>[</c>.</summary>
    public string DisplayName
    {
        get
        {
            if (Kind == BindingKind.Key)
            {
                return ModifierPrefix(Modifiers, display: true) + (KeyDisplay.TryGetValue(Key, out var shown) ? shown : Key.ToString());
            }

            var button = Button switch
            {
                MouseButton.Left => "LMB",
                MouseButton.Right => "RMB",
                MouseButton.Middle => "MMB",
                MouseButton.WheelUp => "Wheel up",
                MouseButton.WheelDown => "Wheel down",
                _ => Button.ToString(),
            };
            return ModifierPrefix(Modifiers, display: true) + button;
        }
    }

    public InputEvent ToInputEvent()
    {
        var ctrl = Modifiers.HasFlag(BindingModifiers.Ctrl);
        var shift = Modifiers.HasFlag(BindingModifiers.Shift);
        var alt = Modifiers.HasFlag(BindingModifiers.Alt);
        var meta = Modifiers.HasFlag(BindingModifiers.Meta);
        return Kind == BindingKind.Key
            ? new InputEventKey { Keycode = Key, CtrlPressed = ctrl, ShiftPressed = shift, AltPressed = alt, MetaPressed = meta }
            : new InputEventMouseButton { ButtonIndex = Button, CtrlPressed = ctrl, ShiftPressed = shift, AltPressed = alt, MetaPressed = meta };
    }

    /// <summary>Canonical order Ctrl+Shift+Alt+Meta; display text calls Meta "Cmd" on macOS.</summary>
    private static string ModifierPrefix(BindingModifiers modifiers, bool display = false)
    {
        var text = "";
        foreach (var modifier in new[] { BindingModifiers.Ctrl, BindingModifiers.Shift, BindingModifiers.Alt, BindingModifiers.Meta })
        {
            if (modifiers.HasFlag(modifier))
            {
                var name = display && modifier == BindingModifiers.Meta && OperatingSystem.IsMacOS() ? "Cmd" : modifier.ToString();
                text += string.Create(CultureInfo.InvariantCulture, $"{name}+");
            }
        }

        return text;
    }
}

using System.Text;
using System.Windows.Input;

namespace AudioSwitch.Models;

/// <summary>
/// Комбинация клавиш в формате Win32 (RegisterHotKey).
/// Значения Modifiers совпадают с MOD_ALT / MOD_CONTROL / MOD_SHIFT / MOD_WIN.
/// </summary>
public sealed record Hotkey(uint Modifiers, int VirtualKey)
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;

    public static Hotkey DefaultOutput { get; } = new(ModControl | ModAlt, 0x78); // Ctrl+Alt+F9

    public static Hotkey DefaultInput { get; } = new(ModControl | ModAlt, 0x79);  // Ctrl+Alt+F10

    public bool IsEmpty => VirtualKey == 0;

    public static Hotkey FromKeyboard(ModifierKeys modifiers, Key key)
    {
        uint mods = 0;
        if (modifiers.HasFlag(ModifierKeys.Control)) mods |= ModControl;
        if (modifiers.HasFlag(ModifierKeys.Alt)) mods |= ModAlt;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mods |= ModShift;
        if (modifiers.HasFlag(ModifierKeys.Windows)) mods |= ModWin;

        return new Hotkey(mods, KeyInterop.VirtualKeyFromKey(key));
    }

    public override string ToString()
    {
        if (IsEmpty) return string.Empty;

        var sb = new StringBuilder();
        void Append(string part)
        {
            if (sb.Length > 0) sb.Append(" + ");
            sb.Append(part);
        }

        if ((Modifiers & ModControl) != 0) Append("Ctrl");
        if ((Modifiers & ModAlt) != 0) Append("Alt");
        if ((Modifiers & ModShift) != 0) Append("Shift");
        if ((Modifiers & ModWin) != 0) Append("Win");

        var key = KeyInterop.KeyFromVirtualKey(VirtualKey);
        var keyName = key switch
        {
            Key.Back => "Backspace",
            Key.Return => "Enter",
            Key.Escape => "Esc",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Prior => "Page Up",
            Key.Next => "Page Down",
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "\\",
            Key.OemTilde => "`",
            _ => new KeyConverter().ConvertToString(key) ?? key.ToString()
        };

        Append(keyName);
        return sb.ToString();
    }
}

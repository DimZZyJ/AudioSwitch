namespace AudioSwitch.Models;

/// <summary>Пользовательские настройки, сохраняемые в %APPDATA%\AudioSwitch\settings.json.</summary>
public sealed class AppSettings
{
    public Hotkey? OutputHotkey { get; set; } = Hotkey.DefaultOutput;

    public Hotkey? InputHotkey { get; set; } = Hotkey.DefaultInput;

    /// <summary>Запускать ли приложение при входе в Windows (ключ автозапуска в реестре).</summary>
    public bool StartWithWindows { get; set; }

    public List<string> FavoriteOutputIds { get; set; } = [];

    public List<string> FavoriteInputIds { get; set; } = [];
}

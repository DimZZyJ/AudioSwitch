using Microsoft.Win32;

namespace AudioSwitch.Services;

/// <summary>
/// Автозапуск через раздел HKCU\Software\Microsoft\Windows\CurrentVersion\Run:
/// значение «AudioSwitch» с путём к текущему исполняемому файлу.
/// HKCU не требует прав администратора.
/// </summary>
public sealed class WindowsStartupService : IWindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "AudioSwitch";

    public void SetEnabled(bool enabled)
    {
        var executablePath = Environment.ProcessPath
                             ?? throw new InvalidOperationException("Не удалось определить путь к AudioSwitch.exe.");

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        var desired = $"\"{executablePath}\"";
        var current = key.GetValue(RunValueName) as string;

        if (enabled)
        {
            // Запись может быть устаревшей (программу переместили или пересобрали) —
            // перезаписываем только при реальном расхождении.
            if (!string.Equals(current, desired, StringComparison.OrdinalIgnoreCase))
                key.SetValue(RunValueName, desired, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }
}

namespace AudioSwitch.Services;

/// <summary>Управление запуском приложения при входе в Windows.</summary>
public interface IWindowsStartupService
{
    /// <summary>Включает или отключает автозапуск. При ошибке доступа к реестру бросает исключение.</summary>
    void SetEnabled(bool enabled);
}

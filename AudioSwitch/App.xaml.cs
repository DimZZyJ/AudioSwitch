using System.ComponentModel;
using System.Threading;
using System.Windows;
using AudioSwitch.Models;
using AudioSwitch.Services;
using AudioSwitch.ViewModels;

namespace AudioSwitch;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\AudioSwitch.SingleInstance";

    private Mutex? _singleInstanceMutex;
    private HotkeyService? _hotkeyService;
    private TrayIconService? _tray;
    private MainWindow? _window;
    private MainViewModel? _viewModel;
    private bool _trayHintShown;

    protected override void OnStartup(StartupEventArgs args)
    {
        base.OnStartup(args);

        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                "AudioSwitch уже запущен — его значок находится в трее.",
                "AudioSwitch",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        SessionEnding += OnSessionEnding;

        try
        {
            Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Не удалось запустить AudioSwitch: " + ex.Message,
                "AudioSwitch",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void Start()
    {
        var settingsService = new SettingsService();
        var audioService = new AudioDeviceService();
        var startupService = new WindowsStartupService();

        _viewModel = new MainViewModel(audioService, settingsService, startupService);
        _viewModel.DeviceSwitched += OnDeviceSwitched;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.ExitRequested += (_, _) => ExitApplication();

        _hotkeyService = new HotkeyService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        RegisterHotkey(DeviceFlow.Output, _viewModel.OutputHotkey, initial: true);
        RegisterHotkey(DeviceFlow.Input, _viewModel.InputHotkey, initial: true);

        _tray = new TrayIconService();
        _tray.OpenRequested += (_, _) => _window?.ShowFromTray();
        _tray.SwitchOutputRequested += (_, _) => _viewModel.SwitchNext(DeviceFlow.Output);
        _tray.SwitchInputRequested += (_, _) => _viewModel.SwitchNext(DeviceFlow.Input);
        _tray.MenuOpening += (_, _) => _viewModel.RefreshDevices();
        _tray.ExitRequested += (_, _) => ExitApplication();
        UpdateTrayState();

        _window = new MainWindow(_viewModel);
        _window.HiddenToTray += OnWindowHiddenToTray;
        MainWindow = _window;
        _window.Show();
    }

    private void OnHotkeyPressed(DeviceFlow flow) => _viewModel?.SwitchNext(flow);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.OutputHotkey):
                RegisterHotkey(DeviceFlow.Output, _viewModel!.OutputHotkey, initial: false);
                UpdateTrayState();
                break;
            case nameof(MainViewModel.InputHotkey):
                RegisterHotkey(DeviceFlow.Input, _viewModel!.InputHotkey, initial: false);
                UpdateTrayState();
                break;
            case nameof(MainViewModel.OutputDefaultName):
            case nameof(MainViewModel.InputDefaultName):
                UpdateTrayState();
                break;
        }
    }

    private void OnDeviceSwitched(object? sender, DeviceSwitchEventArgs e)
    {
        UpdateTrayState();

        // Если окно скрыто, единственный способ показать результат — всплывающее уведомление.
        if (_window is null || _window.IsVisible || _tray is null)
            return;

        _tray.ShowBalloon(
            MainViewModel.FlowTitle(e.Flow),
            e.Success ? e.Message : "Ошибка: " + e.Message);
    }

    private void OnWindowHiddenToTray(object? sender, EventArgs e)
    {
        if (_trayHintShown || _tray is null)
            return;

        _trayHintShown = true;
        _tray.ShowBalloon(
            "AudioSwitch",
            "Приложение свёрнуто в трей. Горячие клавиши переключения устройств продолжают работать.");
    }

    private void RegisterHotkey(DeviceFlow flow, Hotkey? hotkey, bool initial)
    {
        if (_hotkeyService is null || _viewModel is null)
            return;

        var title = MainViewModel.FlowTitle(flow);
        if (_hotkeyService.TryRegister(flow, hotkey, out var error))
            return;

        _viewModel.SetStatus(
            hotkey is null
                ? $"Не удалось снять горячую клавишу «{title}»: {error}"
                : $"Клавиша «{hotkey}» для «{title}» занята: {error}");
    }

    private void UpdateTrayState()
    {
        if (_tray is null || _viewModel is null)
            return;

        _tray.UpdateState(
            _viewModel.OutputDefaultName,
            _viewModel.InputDefaultName,
            _viewModel.OutputHotkey,
            _viewModel.InputHotkey);
    }

    private void ExitApplication()
    {
        if (_window is not null)
            _window.AllowClose = true;

        Shutdown();
    }

    private void OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        // Завершение сеанса Windows не нужно блокировать «закрытием в трей».
        if (_window is not null)
            _window.AllowClose = true;

        Shutdown();
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "Необработанная ошибка: " + e.Exception.Message,
            "AudioSwitch",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs args)
    {
        _tray?.Dispose();
        _hotkeyService?.Dispose();

        if (_singleInstanceMutex is { } mutex)
        {
            mutex.ReleaseMutex();
            mutex.Dispose();
            _singleInstanceMutex = null;
        }

        base.OnExit(args);
    }
}

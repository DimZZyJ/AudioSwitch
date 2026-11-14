using System.Collections.ObjectModel;
using AudioSwitch.Models;
using AudioSwitch.Services;

namespace AudioSwitch.ViewModels;

/// <summary>Основная модель представления окна AudioSwitch.</summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly IAudioDeviceService _audio;
    private readonly ISettingsService _settings;
    private readonly IWindowsStartupService _startup;
    private readonly AppSettings _data;
    private readonly HashSet<string> _outputFavoriteIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _inputFavoriteIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DeviceItemViewModel> _outputMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DeviceItemViewModel> _inputMap = new(StringComparer.OrdinalIgnoreCase);

    private Hotkey? _outputHotkey;
    private Hotkey? _inputHotkey;
    private bool _startWithWindows;
    private string _statusMessage = "Готово";
    private string _outputDefaultName = "Не найдено";
    private string _outputDefaultDescription = string.Empty;
    private string _inputDefaultName = "Не найдено";
    private string _inputDefaultDescription = string.Empty;

    public MainViewModel(IAudioDeviceService audio, ISettingsService settings, IWindowsStartupService startup)
    {
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _startup = startup ?? throw new ArgumentNullException(nameof(startup));

        var loaded = settings.Load();
        var isFirstRun = loaded is null;
        _data = loaded ?? new AppSettings();
        _startWithWindows = _data.StartWithWindows;

        foreach (var id in _data.FavoriteOutputIds)
            _outputFavoriteIds.Add(id);
        foreach (var id in _data.FavoriteInputIds)
            _inputFavoriteIds.Add(id);

        _outputHotkey = isFirstRun ? Hotkey.DefaultOutput : _data.OutputHotkey;
        _inputHotkey = isFirstRun ? Hotkey.DefaultInput : _data.InputHotkey;

        SwitchOutputCommand = new RelayCommand(
            () => SwitchNext(DeviceFlow.Output),
            () => OutputFavorites.Count > 0);
        SwitchInputCommand = new RelayCommand(
            () => SwitchNext(DeviceFlow.Input),
            () => InputFavorites.Count > 0);
        RefreshCommand = new RelayCommand(() =>
        {
            RefreshDevices();
            SetStatus("Список устройств обновлён");
        });
        ExitCommand = new RelayCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));

        RefreshDevices();

        if (isFirstRun)
        {
            var outputDefault = OutputDevices.FirstOrDefault(static item => item.IsDefault);
            if (outputDefault is not null)
                SetFavorite(outputDefault, true, announce: false);

            var inputDefault = InputDevices.FirstOrDefault(static item => item.IsDefault);
            if (inputDefault is not null)
                SetFavorite(inputDefault, true, announce: false);

            SaveSettings();
            SetStatus("Отметьте звёздочкой устройства, между которыми нужно переключаться.");
        }
        else if (_startWithWindows)
        {
            // Путь к exe мог измениться (пересборка, перенос папки) — обновляем ключ автозапуска.
            try
            {
                _startup.SetEnabled(true);
            }
            catch (Exception ex)
            {
                SetStatus($"Не удалось обновить автозапуск: {ex.Message}");
            }
        }
    }

    public ObservableCollection<DeviceItemViewModel> OutputDevices { get; } = [];
    public ObservableCollection<DeviceItemViewModel> OutputFavorites { get; } = [];
    public ObservableCollection<DeviceItemViewModel> InputDevices { get; } = [];
    public ObservableCollection<DeviceItemViewModel> InputFavorites { get; } = [];

    public RelayCommand SwitchOutputCommand { get; }
    public RelayCommand SwitchInputCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ExitCommand { get; }

    /// <summary>Запрос на полное закрытие приложения (обработка — в App).</summary>
    public event EventHandler? ExitRequested;

    public event EventHandler<DeviceSwitchEventArgs>? DeviceSwitched;

    public Hotkey? OutputHotkey
    {
        get => _outputHotkey;
        set
        {
            if (!SetProperty(ref _outputHotkey, value))
                return;

            _data.OutputHotkey = value;
            SaveSettings();
            SetStatus(value is null
                ? "Горячая клавиша переключения вывода отключена"
                : $"Горячая клавиша переключения вывода: {value}");
        }
    }

    public Hotkey? InputHotkey
    {
        get => _inputHotkey;
        set
        {
            if (!SetProperty(ref _inputHotkey, value))
                return;

            _data.InputHotkey = value;
            SaveSettings();
            SetStatus(value is null
                ? "Горячая клавиша переключения ввода отключена"
                : $"Горячая клавиша переключения ввода: {value}");
        }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (!SetProperty(ref _startWithWindows, value))
                return;

            try
            {
                _startup.SetEnabled(value);
            }
            catch (Exception ex)
            {
                // Не удалось изменить ключ реестра — откатываем настройку и галочку в интерфейсе.
                _startWithWindows = !value;
                OnPropertyChanged(nameof(StartWithWindows));
                SetStatus($"Не удалось {(value ? "включить" : "отключить")} автозапуск: {ex.Message}");
                return;
            }

            _data.StartWithWindows = value;
            SaveSettings();
            SetStatus(value
                ? "AudioSwitch будет запускаться при входе в Windows"
                : "Автозапуск при входе в Windows отключён");
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string OutputDefaultName
    {
        get => _outputDefaultName;
        private set => SetProperty(ref _outputDefaultName, value);
    }

    public string OutputDefaultDescription
    {
        get => _outputDefaultDescription;
        private set => SetProperty(ref _outputDefaultDescription, value);
    }

    public string InputDefaultName
    {
        get => _inputDefaultName;
        private set => SetProperty(ref _inputDefaultName, value);
    }

    public string InputDefaultDescription
    {
        get => _inputDefaultDescription;
        private set => SetProperty(ref _inputDefaultDescription, value);
    }

    public int OutputFavoritesCount => OutputFavorites.Count;

    public int InputFavoritesCount => InputFavorites.Count;

    public void RefreshDevices()
    {
        try
        {
            SyncFlow(
                DeviceFlow.Output,
                _audio.GetActiveDevices(DeviceFlow.Output),
                _outputMap,
                OutputDevices,
                OutputFavorites,
                _outputFavoriteIds);

            SyncFlow(
                DeviceFlow.Input,
                _audio.GetActiveDevices(DeviceFlow.Input),
                _inputMap,
                InputDevices,
                InputFavorites,
                _inputFavoriteIds);

            UpdateDefaultDevices();
            RefreshCommandStates();
        }
        catch (Exception ex)
        {
            SetStatus($"Не удалось получить список устройств: {ex.Message}");
        }
    }

    public void SwitchNext(DeviceFlow flow)
    {
        var favorites = flow == DeviceFlow.Output ? OutputFavorites : InputFavorites;
        if (favorites.Count == 0)
        {
            SetStatus(flow == DeviceFlow.Output
                ? "Нет избранных устройств вывода. Отметьте нужные звёздочкой."
                : "Нет избранных устройств ввода. Отметьте нужные звёздочкой.");
            return;
        }

        var current = favorites.FirstOrDefault(static item => item.IsDefault);
        var next = current is null
            ? favorites[0]
            : favorites[(favorites.IndexOf(current) + 1) % favorites.Count];

        if (next.IsDefault)
        {
            SetStatus($"Уже используется: {next.Name}");
            return;
        }

        ApplyDefault(next);
    }

    public void SetStatus(string message) => StatusMessage = message;

    public static string FlowTitle(DeviceFlow flow) =>
        flow == DeviceFlow.Output ? "Вывод звука" : "Ввод звука";

    private void ActivateDevice(DeviceItemViewModel item) => ApplyDefault(item);

    private void ApplyDefault(DeviceItemViewModel item)
    {
        if (item.IsDefault)
            return;

        try
        {
            _audio.SetDefaultDevice(item.Id);
            UpdateDefaultFlags(item.Flow, item.Id);
            SetStatus($"{FlowTitle(item.Flow)}: {item.Name}");
            DeviceSwitched?.Invoke(this, new DeviceSwitchEventArgs(item.Flow, item.Name, success: true));
        }
        catch (Exception ex)
        {
            SetStatus($"Не удалось переключить {FlowTitle(item.Flow).ToLowerInvariant()}: {ex.Message}");
            DeviceSwitched?.Invoke(this, new DeviceSwitchEventArgs(item.Flow, ex.Message, success: false));
            RefreshDevices();
        }

        RefreshCommandStates();
    }

    private void ToggleFavorite(DeviceItemViewModel item) =>
        SetFavorite(item, !item.IsFavorite, announce: true);

    private void SetFavorite(DeviceItemViewModel item, bool value, bool announce)
    {
        if (item.IsFavorite == value)
            return;

        var favoriteIds = item.Flow == DeviceFlow.Output ? _outputFavoriteIds : _inputFavoriteIds;
        var favorites = item.Flow == DeviceFlow.Output ? OutputFavorites : InputFavorites;

        item.IsFavorite = value;

        if (value)
        {
            favoriteIds.Add(item.Id);
            InsertSorted(favorites, item);
            if (announce)
                SetStatus($"«{item.Name}» добавлено в избранное");
        }
        else
        {
            favoriteIds.Remove(item.Id);
            favorites.Remove(item);
            if (announce)
                SetStatus($"«{item.Name}» удалено из избранного");
        }

        RefreshCommandStates(item.Flow);
        if (announce)
            SaveSettings();
    }

    private void SyncFlow(
        DeviceFlow flow,
        IReadOnlyList<AudioDevice> devices,
        Dictionary<string, DeviceItemViewModel> map,
        ObservableCollection<DeviceItemViewModel> all,
        ObservableCollection<DeviceItemViewModel> favorites,
        HashSet<string> favoriteIds)
    {
        var presentIds = devices.Select(static d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var gone in map.Where(pair => !presentIds.Contains(pair.Key))
                     .Select(pair => pair.Value).ToList())
        {
            map.Remove(gone.Id);
            all.Remove(gone);
            favorites.Remove(gone);
        }

        foreach (var device in devices)
        {
            if (map.TryGetValue(device.Id, out var existing))
            {
                existing.Name = device.Name;
                existing.Description = device.Description;
                continue;
            }

            var isFavorite = favoriteIds.Contains(device.Id);
            var item = new DeviceItemViewModel(device, isFavorite, ToggleFavorite, ActivateDevice);
            map.Add(device.Id, item);
            InsertSorted(all, item);

            if (isFavorite)
                InsertSorted(favorites, item);
        }
    }

    private void UpdateDefaultDevices()
    {
        var outputId = _audio.GetDefaultDeviceId(DeviceFlow.Output);
        var inputId = _audio.GetDefaultDeviceId(DeviceFlow.Input);

        UpdateDefaultFlags(DeviceFlow.Output, outputId);
        UpdateDefaultFlags(DeviceFlow.Input, inputId);
    }

    private void UpdateDefaultFlags(DeviceFlow flow, string? defaultId)
    {
        if (flow == DeviceFlow.Output)
        {
            foreach (var item in OutputDevices)
                item.IsDefault = string.Equals(item.Id, defaultId, StringComparison.OrdinalIgnoreCase);

            var device = OutputDevices.FirstOrDefault(static item => item.IsDefault);
            OutputDefaultName = device?.Name ?? "Устройство не найдено";
            OutputDefaultDescription = device?.Description ?? string.Empty;
        }
        else
        {
            foreach (var item in InputDevices)
                item.IsDefault = string.Equals(item.Id, defaultId, StringComparison.OrdinalIgnoreCase);

            var device = InputDevices.FirstOrDefault(static item => item.IsDefault);
            InputDefaultName = device?.Name ?? "Устройство не найдено";
            InputDefaultDescription = device?.Description ?? string.Empty;
        }
    }

    private void RefreshCommandStates(DeviceFlow? flow = null)
    {
        if (flow is null or DeviceFlow.Output)
        {
            SwitchOutputCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(OutputFavoritesCount));
        }

        if (flow is null or DeviceFlow.Input)
        {
            SwitchInputCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(InputFavoritesCount));
        }
    }

    private static void InsertSorted(ObservableCollection<DeviceItemViewModel> collection, DeviceItemViewModel item)
    {
        var index = 0;
        while (index < collection.Count &&
               string.Compare(collection[index].Name, item.Name, StringComparison.CurrentCultureIgnoreCase) < 0)
        {
            index++;
        }

        collection.Insert(index, item);
    }

    private void SaveSettings()
    {
        try
        {
            _data.OutputHotkey = _outputHotkey;
            _data.InputHotkey = _inputHotkey;
            _data.StartWithWindows = _startWithWindows;
            _data.FavoriteOutputIds = [.. _outputFavoriteIds];
            _data.FavoriteInputIds = [.. _inputFavoriteIds];
            _settings.Save(_data);
        }
        catch (Exception ex)
        {
            SetStatus($"Не удалось сохранить настройки: {ex.Message}");
        }
    }
}

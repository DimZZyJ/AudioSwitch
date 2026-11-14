using AudioSwitch.Models;

namespace AudioSwitch.ViewModels;

/// <summary>Строка списка аудиоустройств.</summary>
public sealed class DeviceItemViewModel : ObservableObject
{
    private readonly Action<DeviceItemViewModel> _toggleFavorite;
    private readonly Action<DeviceItemViewModel> _activate;
    private string _name;
    private string _description;
    private bool _isFavorite;
    private bool _isDefault;

    public DeviceItemViewModel(
        AudioDevice device,
        bool isFavorite,
        Action<DeviceItemViewModel> toggleFavorite,
        Action<DeviceItemViewModel> activate)
    {
        Id = device.Id;
        Flow = device.Flow;
        _name = device.Name;
        _description = device.Description;
        _isFavorite = isFavorite;
        _toggleFavorite = toggleFavorite;
        _activate = activate;

        ToggleFavoriteCommand = new RelayCommand(() => _toggleFavorite(this));
        ActivateCommand = new RelayCommand(() => _activate(this));
    }

    public string Id { get; }

    public DeviceFlow Flow { get; }

    public string Name
    {
        get => _name;
        internal set
        {
            if (SetProperty(ref _name, value))
                OnPropertyChanged(nameof(HasDescription));
        }
    }

    public string Description
    {
        get => _description;
        internal set
        {
            if (SetProperty(ref _description, value))
                OnPropertyChanged(nameof(HasDescription));
        }
    }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public bool IsFavorite
    {
        get => _isFavorite;
        internal set => SetProperty(ref _isFavorite, value);
    }

    public bool IsDefault
    {
        get => _isDefault;
        internal set => SetProperty(ref _isDefault, value);
    }

    public RelayCommand ToggleFavoriteCommand { get; }

    public RelayCommand ActivateCommand { get; }
}

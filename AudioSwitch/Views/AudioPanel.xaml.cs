using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AudioSwitch.Models;

namespace AudioSwitch.Views;

/// <summary>Панель одного направления: активное устройство, горячая клавиша и списки устройств.</summary>
public partial class AudioPanel : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(AudioPanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle), typeof(string), typeof(AudioPanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(Geometry), typeof(AudioPanel), new PropertyMetadata(null));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(AudioPanel), new PropertyMetadata(null));

    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey),
        typeof(Hotkey),
        typeof(AudioPanel),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty DefaultNameProperty = DependencyProperty.Register(
        nameof(DefaultName), typeof(string), typeof(AudioPanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DefaultDescriptionProperty = DependencyProperty.Register(
        nameof(DefaultDescription), typeof(string), typeof(AudioPanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SwitchCommandProperty = DependencyProperty.Register(
        nameof(SwitchCommand), typeof(ICommand), typeof(AudioPanel), new PropertyMetadata(null));

    public static readonly DependencyProperty DevicesProperty = DependencyProperty.Register(
        nameof(Devices), typeof(IEnumerable), typeof(AudioPanel), new PropertyMetadata(null));

    public static readonly DependencyProperty FavoritesProperty = DependencyProperty.Register(
        nameof(Favorites), typeof(IEnumerable), typeof(AudioPanel), new PropertyMetadata(null));

    public static readonly DependencyProperty FavoriteCountProperty = DependencyProperty.Register(
        nameof(FavoriteCount), typeof(int), typeof(AudioPanel), new PropertyMetadata(0));

    public AudioPanel()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public Geometry? Glyph
    {
        get => (Geometry?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public Brush? AccentBrush
    {
        get => (Brush?)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Hotkey? Hotkey
    {
        get => (Hotkey?)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public string DefaultName
    {
        get => (string)GetValue(DefaultNameProperty);
        set => SetValue(DefaultNameProperty, value);
    }

    public string DefaultDescription
    {
        get => (string)GetValue(DefaultDescriptionProperty);
        set => SetValue(DefaultDescriptionProperty, value);
    }

    public ICommand? SwitchCommand
    {
        get => (ICommand?)GetValue(SwitchCommandProperty);
        set => SetValue(SwitchCommandProperty, value);
    }

    public IEnumerable? Devices
    {
        get => (IEnumerable?)GetValue(DevicesProperty);
        set => SetValue(DevicesProperty, value);
    }

    public IEnumerable? Favorites
    {
        get => (IEnumerable?)GetValue(FavoritesProperty);
        set => SetValue(FavoritesProperty, value);
    }

    public int FavoriteCount
    {
        get => (int)GetValue(FavoriteCountProperty);
        set => SetValue(FavoriteCountProperty, value);
    }
}

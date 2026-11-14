using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AudioSwitch.Models;

namespace AudioSwitch.Views;

/// <summary>
/// Поле ввода глобальной горяче клавиши: достаточно кликнуть и нажать комбинацию.
/// </summary>
public partial class HotkeyBox : UserControl
{
    private const string CaptureHint = "Нажмите комбинацию клавиш…";

    private static readonly DependencyPropertyKey HintStatePropertyKey = DependencyProperty.RegisterReadOnly(
        "HintState", typeof(string), typeof(HotkeyBox), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HintStateProperty = HintStatePropertyKey.DependencyProperty;

    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey),
        typeof(Hotkey),
        typeof(HotkeyBox),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnHotkeyChanged));

    private bool _isCapturing;

    public HotkeyBox()
    {
        InitializeComponent();
        UpdateVisualState();
    }

    public Hotkey? Hotkey
    {
        get => (Hotkey?)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public string HintState => (string)GetValue(HintStateProperty);

    public event EventHandler? HotkeyChanged;

    private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (HotkeyBox)d;
        box.UpdateVisualState();
        box.HotkeyChanged?.Invoke(box, EventArgs.Empty);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e) => Focus();

    private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _isCapturing = true;
        UpdateVisualState();
    }

    private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _isCapturing = false;
        UpdateVisualState();
    }

    private void OnPreviewKeyDownHandler(object sender, KeyEventArgs e)
    {
        if (!_isCapturing)
            return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        switch (key)
        {
            case Key.LeftShift or Key.RightShift
                or Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt
                or Key.LWin or Key.RWin or Key.Apps:
                e.Handled = true;
                return;

            case Key.Escape:
                e.Handled = true;
                Keyboard.ClearFocus();
                return;

            case Key.Delete or Key.Back:
                e.Handled = true;
                SetCurrentValue(HotkeyProperty, null);
                UpdateVisualState();
                return;

            case Key.Tab:
                return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None && !IsSingleKeyAllowed(key))
        {
            SetValue(HintStatePropertyKey, "Нужна комбинация с Ctrl / Alt / Shift / Win либо клавиша F1–F24");
            UpdateVisualState();
            e.Handled = true;
            return;
        }

        e.Handled = true;
        SetCurrentValue(HotkeyProperty, Hotkey.FromKeyboard(modifiers, key));
        Keyboard.ClearFocus();
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        SetCurrentValue(HotkeyProperty, null);
        e.Handled = true;
    }

    private void UpdateVisualState()
    {
        if (_isCapturing)
        {
            HintText.Text = string.IsNullOrEmpty(HintState) ? CaptureHint : HintState;
            HintText.Foreground = (Brush)FindResource(
                string.IsNullOrEmpty(HintState) ? "MutedBrush" : "WarningBrush");
            HintText.Visibility = Visibility.Visible;
            ValueText.Visibility = Visibility.Collapsed;
            ClearButton.Visibility = Visibility.Collapsed;
            Root.BorderBrush = (Brush)FindResource("AccentBrush");
            return;
        }

        SetValue(HintStatePropertyKey, string.Empty);
        var hotkey = Hotkey;

        if (hotkey is { IsEmpty: false })
        {
            ValueText.Text = hotkey.ToString();
            ValueText.Visibility = Visibility.Visible;
            HintText.Visibility = Visibility.Collapsed;
            ClearButton.Visibility = Visibility.Visible;
        }
        else
        {
            ValueText.Visibility = Visibility.Collapsed;
            HintText.Text = "Не задано — кликните и нажмите клавиши";
            HintText.Foreground = (Brush)FindResource("MutedBrush");
            HintText.Visibility = Visibility.Visible;
            ClearButton.Visibility = Visibility.Collapsed;
        }

        Root.BorderBrush = (Brush)FindResource("BorderBrush");
    }

    private static bool IsSingleKeyAllowed(Key key) =>
        key is >= Key.F1 and <= Key.F24
            or Key.VolumeMute or Key.VolumeDown or Key.VolumeUp
            or Key.MediaNextTrack or Key.MediaPlayPause or Key.MediaPreviousTrack or Key.MediaStop;
}

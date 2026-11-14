using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using AudioSwitch.ViewModels;

namespace AudioSwitch;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _refreshTimer;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _refreshTimer.Tick += (_, _) => _viewModel.RefreshDevices();
        _refreshTimer.Start();
    }

    /// <summary>Разрешает реальное закрытие окна (используется при выходе из приложения).</summary>
    public bool AllowClose { get; set; }

    /// <summary>Окно скрыто в трей.</summary>
    public event EventHandler? HiddenToTray;

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
    }

    public void HideToTray()
    {
        if (!IsVisible)
            return;

        Hide();

        // Если свернули кнопкой «Свернуть», состояние Minimized осталось бы
        // и окно при следующем открытии появилось свёрнутым.
        WindowState = WindowState.Normal;
        HiddenToTray?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (AllowClose)
        {
            _refreshTimer.Stop();
            base.OnClosing(e);
            return;
        }

        // Крестик не закрывает приложение, а прячет его в трей.
        e.Cancel = true;
        HideToTray();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (WindowState == WindowState.Minimized)
            HideToTray();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        _viewModel.RefreshDevices();
    }

    private void OnMinimizeToTrayClick(object sender, RoutedEventArgs e) => HideToTray();

    private void OnCloseClick(object sender, RoutedEventArgs e) => HideToTray();
}

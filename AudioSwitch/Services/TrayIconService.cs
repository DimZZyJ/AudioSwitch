using AudioSwitch.Models;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace AudioSwitch.Services;

/// <summary>Иконка в области уведомлений и её контекстное меню.</summary>
public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Forms.ToolStripMenuItem _outputHeaderItem;
    private readonly Forms.ToolStripMenuItem _outputSwitchItem;
    private readonly Forms.ToolStripMenuItem _inputHeaderItem;
    private readonly Forms.ToolStripMenuItem _inputSwitchItem;
    private Drawing.Icon? _trayIcon;
    private bool _disposed;

    public TrayIconService()
    {
        _menu = new Forms.ContextMenuStrip
        {
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Font = new Drawing.Font("Segoe UI", 9.5f),
            Renderer = new Forms.ToolStripProfessionalRenderer(new DarkMenuColorTable())
        };

        _menu.Opening += (_, _) => MenuOpening?.Invoke(this, EventArgs.Empty);

        var openItem = CreateItem("Открыть окно", () => OpenRequested?.Invoke(this, EventArgs.Empty));
        openItem.Font = new Drawing.Font(_menu.Font, Drawing.FontStyle.Bold);

        _outputHeaderItem = CreateHeader();
        _outputSwitchItem = CreateItem("Переключить вывод", () =>
            SwitchOutputRequested?.Invoke(this, EventArgs.Empty));
        _inputHeaderItem = CreateHeader();
        _inputSwitchItem = CreateItem("Переключить ввод", () =>
            SwitchInputRequested?.Invoke(this, EventArgs.Empty));
        var exitItem = CreateItem("Выход", () => ExitRequested?.Invoke(this, EventArgs.Empty));

        _menu.Items.AddRange(
        [
            openItem,
            new Forms.ToolStripSeparator(),
            _outputHeaderItem,
            _outputSwitchItem,
            _inputHeaderItem,
            _inputSwitchItem,
            new Forms.ToolStripSeparator(),
            exitItem
        ]);

        _trayIcon = LoadIcon();

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "AudioSwitch",
            Visible = true,
            ContextMenuStrip = _menu
        };

        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OpenRequested;

    public event EventHandler? SwitchOutputRequested;

    public event EventHandler? SwitchInputRequested;

    public event EventHandler? ExitRequested;

    public event EventHandler? MenuOpening;

    public void UpdateState(string outputName, string inputName, Hotkey? outputHotkey, Hotkey? inputHotkey)
    {
        if (_disposed)
            return;

        _outputHeaderItem.Text = "Вывод: " + Shorten(outputName, 42);
        _inputHeaderItem.Text = "Ввод: " + Shorten(inputName, 42);

        _outputSwitchItem.ShortcutKeyDisplayString = outputHotkey?.ToString() ?? "не задана";
        _inputSwitchItem.ShortcutKeyDisplayString = inputHotkey?.ToString() ?? "не задана";

        _notifyIcon.Text = Shorten(
            $"AudioSwitch · вывод: {outputName} · ввод: {inputName}", 63);
    }

    public void ShowBalloon(string title, string text)
    {
        if (_disposed)
            return;

        _notifyIcon.BalloonTipTitle = Shorten(title, 63);
        _notifyIcon.BalloonTipText = Shorten(text, 255);
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(2500);
    }

    private static Forms.ToolStripMenuItem CreateItem(string text, Action action)
    {
        var item = new Forms.ToolStripMenuItem(text) { ForeColor = Drawing.Color.FromArgb(231, 234, 240) };
        item.Click += (_, _) => action();
        return item;
    }

    private static Forms.ToolStripMenuItem CreateHeader() => new()
    {
        Enabled = false,
        ForeColor = Drawing.Color.FromArgb(140, 148, 163)
    };

    private static Drawing.Icon LoadIcon()
    {
        try
        {
            var resource = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/app.ico"));
            if (resource is not null)
            {
                using var stream = resource.Stream;
                using var icon = new Drawing.Icon(stream);
                return new Drawing.Icon(icon, Forms.SystemInformation.SmallIconSize);
            }
        }
        catch
        {
            // Ниже используем системную иконку.
        }

        return (Drawing.Icon)Drawing.SystemIcons.Application.Clone();
    }

    private static string Shorten(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private sealed class DarkMenuColorTable : Forms.ProfessionalColorTable
    {
        private static readonly Drawing.Color Background = Drawing.Color.FromArgb(28, 32, 39);
        private static readonly Drawing.Color Selection = Drawing.Color.FromArgb(48, 56, 70);
        private static readonly Drawing.Color Border = Drawing.Color.FromArgb(55, 62, 76);

        public override Drawing.Color ToolStripDropDownBackground => Background;
        public override Drawing.Color ImageMarginGradientBegin => Background;
        public override Drawing.Color ImageMarginGradientMiddle => Background;
        public override Drawing.Color ImageMarginGradientEnd => Background;
        public override Drawing.Color MenuBorder => Border;
        public override Drawing.Color MenuItemBorder => Drawing.Color.FromArgb(76, 141, 255);
        public override Drawing.Color MenuItemSelected => Selection;
        public override Drawing.Color MenuItemSelectedGradientBegin => Selection;
        public override Drawing.Color MenuItemSelectedGradientEnd => Selection;
        public override Drawing.Color MenuItemPressedGradientBegin => Selection;
        public override Drawing.Color MenuItemPressedGradientEnd => Selection;
        public override Drawing.Color SeparatorDark => Drawing.Color.FromArgb(56, 63, 77);
        public override Drawing.Color SeparatorLight => Background;
    }
}

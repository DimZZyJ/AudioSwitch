using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using AudioSwitch.Models;

namespace AudioSwitch.Services;

/// <summary>
/// Регистрирует глобальные горячие клавиши в отдельном message-only окне,
/// поэтому они продолжают работать, когда главное окно скрыто в трее.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private const int OutputHotkeyId = 0xA501;
    private const int InputHotkeyId = 0xA502;

    private readonly object _sync = new();
    private readonly HwndSource _source;
    private readonly Dictionary<DeviceFlow, Hotkey?> _registered = new();
    private bool _disposed;

    public HotkeyService()
    {
        var parameters = new HwndSourceParameters("AudioSwitch.HotkeyWindow")
        {
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE
            WindowStyle = 0,
            ExtendedWindowStyle = 0,
            Width = 0,
            Height = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    public event Action<DeviceFlow>? HotkeyPressed;

    public bool TryRegister(DeviceFlow flow, Hotkey? hotkey, out string? error)
    {
        error = null;
        if (hotkey is { IsEmpty: true })
            hotkey = null;

        lock (_sync)
        {
            if (_disposed)
            {
                error = "Служба горячих клавиш уже остановлена.";
                return false;
            }

            var previous = _registered.GetValueOrDefault(flow);
            if (Equals(previous, hotkey))
                return true;

            var id = flow == DeviceFlow.Output ? OutputHotkeyId : InputHotkeyId;

            if (previous is not null)
                UnregisterHotKey(_source.Handle, id);

            if (hotkey is not null &&
                !RegisterHotKey(_source.Handle, id, hotkey.Modifiers | ModNoRepeat, (uint)hotkey.VirtualKey))
            {
                var code = Marshal.GetLastWin32Error();
                error = new Win32Exception(code).Message;

                // Возвращаем прежнюю комбинацию, чтобы не потерять рабочее переключение.
                if (previous is not null &&
                    RegisterHotKey(_source.Handle, id, previous.Modifiers | ModNoRepeat, (uint)previous.VirtualKey))
                {
                    _registered[flow] = previous;
                }
                else
                {
                    _registered[flow] = null;
                }

                return false;
            }

            _registered[flow] = hotkey;
            return true;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
            return IntPtr.Zero;

        switch (wParam.ToInt32())
        {
            case OutputHotkeyId:
                handled = true;
                HotkeyPressed?.Invoke(DeviceFlow.Output);
                break;
            case InputHotkeyId:
                handled = true;
                HotkeyPressed?.Invoke(DeviceFlow.Input);
                break;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var (flow, hotkey) in _registered)
            {
                if (hotkey is not null)
                {
                    var id = flow == DeviceFlow.Output ? OutputHotkeyId : InputHotkeyId;
                    UnregisterHotKey(_source.Handle, id);
                }
            }

            _registered.Clear();
        }

        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

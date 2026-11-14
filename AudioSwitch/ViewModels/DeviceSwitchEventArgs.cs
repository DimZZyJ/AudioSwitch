using AudioSwitch.Models;

namespace AudioSwitch.ViewModels;

public sealed class DeviceSwitchEventArgs : EventArgs
{
    public DeviceSwitchEventArgs(DeviceFlow flow, string message, bool success)
    {
        Flow = flow;
        Message = message;
        Success = success;
    }

    public DeviceFlow Flow { get; }

    public string Message { get; }

    public bool Success { get; }
}

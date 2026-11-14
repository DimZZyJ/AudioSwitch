using AudioSwitch.Models;

namespace AudioSwitch.Services;

public interface IAudioDeviceService
{
    IReadOnlyList<AudioDevice> GetActiveDevices(DeviceFlow flow);

    string? GetDefaultDeviceId(DeviceFlow flow);

    void SetDefaultDevice(string deviceId);
}

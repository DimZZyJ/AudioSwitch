using System.Runtime.InteropServices;
using AudioSwitch.Models;

namespace AudioSwitch.Services;

/// <summary>Работа с устройствами через Windows Core Audio (MMDevice API + IPolicyConfig).</summary>
public sealed class AudioDeviceService : IAudioDeviceService
{
    private readonly object _sync = new();
    private IPolicyConfig? _policyConfig;

    public IReadOnlyList<AudioDevice> GetActiveDevices(DeviceFlow flow)
    {
        lock (_sync)
        {
            var result = new List<AudioDevice>();
            IMMDeviceEnumerator? enumerator = null;
            IMMDeviceCollection? collection = null;

            try
            {
                enumerator = CoreAudioInterop.CreateEnumerator();
                CoreAudioInterop.Check(enumerator.EnumAudioEndpoints(
                    ToDataFlow(flow), CoreAudioInterop.DeviceStateActive, out collection));
                CoreAudioInterop.Check(collection.GetCount(out var count));

                for (var i = 0; i < count; i++)
                {
                    IMMDevice? device = null;
                    IPropertyStore? store = null;
                    try
                    {
                        if (collection.Item(i, out device) < 0 || device is null)
                            continue;

                        CoreAudioInterop.Check(device.GetId(out var id));
                        CoreAudioInterop.Check(device.OpenPropertyStore(CoreAudioInterop.StgmRead, out store));

                        var friendlyName = CoreAudioInterop.GetStringProperty(
                            store, CoreAudioInterop.PkeyDeviceFriendlyName);
                        var description = CoreAudioInterop.GetStringProperty(
                            store, CoreAudioInterop.PkeyDeviceInterfaceFriendlyName);

                        if (string.IsNullOrWhiteSpace(friendlyName))
                            friendlyName = description ?? "Аудиоустройство";

                        result.Add(new AudioDevice(
                            id,
                            friendlyName.Trim(),
                            (description ?? string.Empty).Trim(),
                            flow));
                    }
                    catch (COMException)
                    {
                        // Одно проблемное устройство не должно ломать весь список.
                    }
                    finally
                    {
                        ReleaseComObject(store);
                        ReleaseComObject(device);
                    }
                }
            }
            finally
            {
                ReleaseComObject(collection);
                ReleaseComObject(enumerator);
            }

            return result
                .OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(d => d.Description, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }

    public string? GetDefaultDeviceId(DeviceFlow flow)
    {
        lock (_sync)
        {
            IMMDeviceEnumerator? enumerator = null;
            IMMDevice? device = null;
            try
            {
                enumerator = CoreAudioInterop.CreateEnumerator();
                CoreAudioInterop.Check(enumerator.GetDefaultAudioEndpoint(
                    ToDataFlow(flow), ERole.Multimedia, out device));
                CoreAudioInterop.Check(device.GetId(out var id));
                return id;
            }
            catch (COMException)
            {
                return null;
            }
            finally
            {
                ReleaseComObject(device);
                ReleaseComObject(enumerator);
            }
        }
    }

    public void SetDefaultDevice(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        lock (_sync)
        {
            _policyConfig ??= CoreAudioInterop.CreatePolicyConfig();

            CoreAudioInterop.Check(_policyConfig.SetDefaultEndpoint(deviceId, ERole.Console));
            CoreAudioInterop.Check(_policyConfig.SetDefaultEndpoint(deviceId, ERole.Multimedia));
            CoreAudioInterop.Check(_policyConfig.SetDefaultEndpoint(deviceId, ERole.Communications));
        }
    }

    private static EDataFlow ToDataFlow(DeviceFlow flow) =>
        flow == DeviceFlow.Output ? EDataFlow.Render : EDataFlow.Capture;

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
            Marshal.FinalReleaseComObject(value);
    }
}

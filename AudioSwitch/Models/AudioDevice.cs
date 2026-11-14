namespace AudioSwitch.Models;

/// <summary>Активное аудиоустройство, полученное из Windows Core Audio.</summary>
public sealed record AudioDevice(string Id, string Name, string Description, DeviceFlow Flow);

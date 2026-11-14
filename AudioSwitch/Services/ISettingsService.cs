using System.Text.Json;
using AudioSwitch.Models;

namespace AudioSwitch.Services;

public interface ISettingsService
{
    string FilePath { get; }

    AppSettings? Load();

    void Save(AppSettings settings);
}

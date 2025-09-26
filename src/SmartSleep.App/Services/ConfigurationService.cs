using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SmartSleep.App.Models;

namespace SmartSleep.App.Services;

public class ConfigurationService
{
    private readonly string _portableConfigPath;
    private readonly string _legacyConfigPath;
    private readonly JsonSerializerOptions _serializerOptions;

    public ConfigurationService()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
        _portableConfigPath = Path.Combine(baseDirectory, "config.json");
        _legacyConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartSleep", "config.json");
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<AppConfig> LoadAsync()
    {
        try
        {
            var path = ResolveConfigPath();
            if (path == null)
            {
                return AppConfig.CreateDefault();
            }

            await using var stream = File.OpenRead(path);
            var config = await JsonSerializer.DeserializeAsync<AppConfig>(stream, _serializerOptions).ConfigureAwait(false);
            return config ?? AppConfig.CreateDefault();
        }
        catch
        {
            return AppConfig.CreateDefault();
        }
    }

    public async Task SaveAsync(AppConfig config)
    {
        if (await TrySaveAsync(_portableConfigPath, config).ConfigureAwait(false))
        {
            return;
        }

        await TrySaveAsync(_legacyConfigPath, config).ConfigureAwait(false);
    }

    private string? ResolveConfigPath()
    {
        if (File.Exists(_portableConfigPath))
        {
            return _portableConfigPath;
        }

        if (File.Exists(_legacyConfigPath))
        {
            return _legacyConfigPath;
        }

        return null;
    }

    private async Task<bool> TrySaveAsync(string path, AppConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, config, _serializerOptions).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

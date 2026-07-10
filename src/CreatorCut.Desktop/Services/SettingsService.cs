using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CreatorCut.Desktop.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;

    public SettingsService(string settingsDirectory)
    {
        Directory.CreateDirectory(settingsDirectory);
        _filePath = Path.Combine(settingsDirectory, "settings.json");
    }

    public T Load<T>(string section, T defaultValue) where T : class
    {
        try
        {
            var json = File.ReadAllText(_filePath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(section, out var sectionEl))
            {
                var value = JsonSerializer.Deserialize<T>(sectionEl.GetRawText(), JsonOptions);
                if (value != null) return value;
            }
        }
        catch
        {
        }

        return defaultValue;
    }

    public void Save<T>(string section, T value)
    {
        try
        {
            var existing = File.Exists(_filePath)
                ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    File.ReadAllText(_filePath), JsonOptions) ?? []
                : [];

            existing[section] = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(value, JsonOptions), JsonOptions);

            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(existing, JsonOptions));
            File.Move(tmp, _filePath, overwrite: true);
        }
        catch
        {
            var fresh = new Dictionary<string, JsonElement>
            {
                [section] = JsonSerializer.Deserialize<JsonElement>(
                    JsonSerializer.Serialize(value, JsonOptions), JsonOptions),
            };
            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(fresh, JsonOptions));
            File.Move(tmp, _filePath, overwrite: true);
        }
    }
}

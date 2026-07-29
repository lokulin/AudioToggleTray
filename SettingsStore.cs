using System;
using System.IO;
using System.Text.Json;

namespace AudioToggleTray;

internal class AppSettings
{
    public string? PrimaryDeviceId { get; set; }
    public string? SecondaryDeviceId { get; set; }
}

internal static class SettingsStore
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioToggleTray");

    private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch
        {
            // Fall through to defaults if the file is missing or corrupt.
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Non-fatal: worst case, the device selection isn't remembered next launch.
        }
    }
}

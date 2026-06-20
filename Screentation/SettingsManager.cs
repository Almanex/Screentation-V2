using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.UI;

namespace Screentation;

public class SettingsModel
{
    public string ExportPath { get; set; } = string.Empty;
    public string ExportFormat { get; set; } = "PNG";
    public int ExportQuality { get; set; } = 90;
    public string NamingTemplate { get; set; } = "screen_[N]";
    public string ThemeChoice { get; set; } = "Default"; // "Default", "Light", "Dark"
    public float StrokeThickness { get; set; } = 3.0f;
    
    // Save color as hex
    public string StrokeColorHex { get; set; } = "#FFFF0000"; // Red

    [JsonIgnore]
    public Color StrokeColor
    {
        get => ParseHexColor(StrokeColorHex);
        set => StrokeColorHex = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
    }

    public bool HasFill { get; set; } = true;

    private static Color ParseHexColor(string hex)
    {
        try
        {
            hex = hex.Replace("#", "");
            if (hex.Length == 8)
            {
                byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }
            else if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return Color.FromArgb(255, r, g, b);
            }
        }
        catch
        {
            // Fallback
        }
        return Microsoft.UI.Colors.Red;
    }
}

public static class SettingsManager
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Screentation"
    );
    
    private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");
    
    public static SettingsModel Current { get; private set; } = new();

    static SettingsManager()
    {
        Load();
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var settings = JsonSerializer.Deserialize<SettingsModel>(json);
                if (settings != null)
                {
                    Current = settings;
                    // If export path is empty, initialize it to user's Pictures folder
                    if (string.IsNullOrEmpty(Current.ExportPath))
                    {
                        Current.ExportPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    }
                    return;
                }
            }
        }
        catch
        {
            // Ignore load errors and use defaults
        }

        Current = new SettingsModel
        {
            ExportPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };
    }

    public static void Save()
    {
        try
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Current, options);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}

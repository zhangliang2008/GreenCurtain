using System;
using System.IO;
using System.Text.Json;

namespace GreenCurtain
{
    public class AppSettings
    {
        public byte ColorR { get; set; } = 50;
        public byte ColorG { get; set; } = 205;
        public byte ColorB { get; set; } = 50;
        public double Opacity { get; set; } = 0.5;
        public byte CenterBrightness { get; set; } = 128;
        public byte EdgeBrightness { get; set; } = 176;
        public int ScreenIndex { get; set; } = 0;
        
        // 快捷键设置
        public int ExitHotkeyModifiers { get; set; } = 1; // Alt
        public int ExitHotkeyKey { get; set; } = 81; // Q
        public int ToggleHotkeyModifiers { get; set; } = 1; // Alt
        public int ToggleHotkeyKey { get; set; } = 65; // A

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GreenCurtain",
            "config.json"
        );

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
            }
        }
    }
}

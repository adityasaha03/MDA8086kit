using System;
using System.Collections.Generic;
using System.IO;

namespace Mda8086Kit.Io
{
    public class UserSettingsStore
    {
        private readonly string _settingsFilePath;

        public UserSettingsStore()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localAppData, "MDA8086_Kit");
            _settingsFilePath = Path.Combine(appFolder, "settings.ini");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
        }

        public Dictionary<string, string> LoadSettings()
        {
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(_settingsFilePath))
            {
                return settings;
            }

            try
            {
                foreach (var line in File.ReadAllLines(_settingsFilePath))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    {
                        continue;
                    }

                    var equalsIndex = trimmed.IndexOf('=');
                    if (equalsIndex > 0)
                    {
                        var key = trimmed.Substring(0, equalsIndex).Trim();
                        var value = trimmed.Substring(equalsIndex + 1).Trim();
                        settings[key] = value;
                    }
                }
            }
            catch
            {
                // Fall back to defaults on corrupt/unreadable file
            }

            return settings;
        }

        public void SaveSettings(Dictionary<string, string> settings)
        {
            var tempFile = _settingsFilePath + ".tmp";
            
            try
            {
                using (var writer = new StreamWriter(tempFile))
                {
                    writer.WriteLine("; MDA8086 Kit User Settings");
                    writer.WriteLine($"; Last Updated: {DateTime.Now}");
                    writer.WriteLine();

                    foreach (var kvp in settings)
                    {
                        writer.WriteLine($"{kvp.Key}={kvp.Value}");
                    }
                }

                // Atomic replace
                if (File.Exists(_settingsFilePath))
                {
                    File.Replace(tempFile, _settingsFilePath, null);
                }
                else
                {
                    File.Move(tempFile, _settingsFilePath);
                }
            }
            catch
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                throw;
            }
        }
    }
}

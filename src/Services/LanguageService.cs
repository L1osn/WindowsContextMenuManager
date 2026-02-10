using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ContextMenuManager.Services
{
    /// <summary>
    /// Persists and applies UI language. Stored under %APPDATA%\ContextMenuManager\settings.json.
    /// </summary>
    public static class LanguageService
    {
        private const string SettingsFileName = "settings.json";
        private const string KeyLanguage = "Language";

        public static readonly IReadOnlyList<(string Culture, string DisplayName)> SupportedLanguages = new[]
        {
            ("en", "English"),
            ("zh-CN", "中文")
        };

        public static string GetSavedLanguage()
        {
            try
            {
                var path = GetSettingsPath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return "en";

                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(KeyLanguage, out var prop))
                {
                    var value = prop.GetString();
                    if (!string.IsNullOrEmpty(value) && SupportedLanguages.Any(x => x.Culture.Equals(value, StringComparison.OrdinalIgnoreCase)))
                        return value;
                }
            }
            catch { }

            return "en";
        }

        public static void SaveLanguage(string cultureName)
        {
            try
            {
                var path = GetSettingsPath();
                if (string.IsNullOrEmpty(path)) return;

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var dict = new Dictionary<string, string>();
                if (File.Exists(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path);
                        using var doc = JsonDocument.Parse(json);
                        foreach (var prop in doc.RootElement.EnumerateObject())
                            dict[prop.Name] = prop.Value.GetString() ?? "";
                    }
                    catch { }
                }

                dict[KeyLanguage] = cultureName ?? "en";
                var newJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, newJson);
            }
            catch { }
        }

        /// <summary>
        /// Applies the saved language to the current thread and default thread culture. Call once at app startup.
        /// </summary>
        public static void ApplySavedLanguage()
        {
            var cultureName = GetSavedLanguage();
            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
            }
            catch
            {
                // fallback to en
            }
        }

        /// <summary>
        /// Saves the new language and restarts the application so the new language takes effect.
        /// </summary>
        public static void SetLanguageAndRestart(string cultureName)
        {
            SaveLanguage(cultureName);
            var processPath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(processPath))
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }

            var entryLocation = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(entryLocation))
            {
                // Running via dotnet (e.g. dotnet run or VS): start dotnet with the entry assembly path
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = processPath,
                    Arguments = entryLocation,
                    UseShellExecute = false
                });
            }
            else
            {
                // Single-file exe or process path is our app: start the exe directly
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = processPath,
                    UseShellExecute = true
                });
            }

            System.Windows.Application.Current.Shutdown();
        }

        private static string? GetSettingsPath()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrEmpty(appData)) return null;
                return Path.Combine(appData, "ContextMenuManager", SettingsFileName);
            }
            catch
            {
                return null;
            }
        }
    }
}

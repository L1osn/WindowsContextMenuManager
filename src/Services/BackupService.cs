using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextMenuManager.Models;

namespace ContextMenuManager.Services
{
    /// <summary>Backup and restore service.</summary>
    public class BackupService
    {
        private readonly string _backupDir;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _appDataRoot;
        private const string FirstRunFlagFileName = "FirstRunDone.flag";

        public BackupService()
        {
            _appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ContextMenuManager");
            _backupDir = Path.Combine(_appDataRoot, "Backups");

            if (!Directory.Exists(_backupDir))
                Directory.CreateDirectory(_backupDir);
        }

        /// <summary>On first run, saves current config as default backup and sets flag for later rollback.</summary>
        public void EnsureDefaultBackupIfFirstRun(List<ContextMenuItem> items)
        {
            var flagPath = Path.Combine(_appDataRoot, FirstRunFlagFileName);
            if (File.Exists(flagPath) || items == null || items.Count == 0)
                return;

            try
            {
                CreateBackup(items, "First run - default config (can rollback)");
                File.WriteAllText(flagPath, DateTime.Now.ToString("O"));
            }
            catch
            {
                // Ignore failure, do not block startup
            }
        }

        /// <summary>Create a backup.</summary>
        public BackupRecord CreateBackup(List<ContextMenuItem> items, string description = "")
        {
            var record = new BackupRecord
            {
                Timestamp = DateTime.Now,
                Description = string.IsNullOrEmpty(description) ? $"Backup - {DateTime.Now:yyyy-MM-dd HH:mm:ss}" : description,
                Entries = items.Select(i => new BackupEntry
                {
                    Id = i.Id,
                    RegistryPath = i.RegistryPath,
                    ItemType = i.ItemType,
                    DisableMethod = i.DisableMethod,
                    Clsid = i.Clsid,
                    WasEnabled = i.IsEnabled,
                    Scenario = i.Scenario
                }).ToList()
            };

            var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            record.FilePath = Path.Combine(_backupDir, fileName);

            var json = JsonSerializer.Serialize(record, JsonOptions);
            File.WriteAllText(record.FilePath, json);

            return record;
        }

        /// <summary>Get all backups (newest first).</summary>
        public List<BackupRecord> GetBackups()
        {
            var backups = new List<BackupRecord>();

            try
            {
                foreach (var file in Directory.GetFiles(_backupDir, "backup_*.json")
                    .OrderByDescending(f => f))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var record = JsonSerializer.Deserialize<BackupRecord>(json, JsonOptions);
                        if (record != null)
                        {
                            record.FilePath = file;
                            backups.Add(record);
                        }
                    }
                    catch
                    {
                        // Skip corrupted backup file
                    }
                }
            }
            catch
            {
                // Directory access failed
            }

            return backups;
        }

        /// <summary>Load a backup from file.</summary>
        public BackupRecord? LoadBackup(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var record = JsonSerializer.Deserialize<BackupRecord>(json, JsonOptions);
                if (record != null)
                    record.FilePath = filePath;
                return record;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Get the latest backup.</summary>
        public BackupRecord? GetLatestBackup()
        {
            var backups = GetBackups();
            return backups.FirstOrDefault();
        }

        /// <summary>Delete a backup file.</summary>
        public void DeleteBackup(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch { }
        }

        /// <summary>Remove old backups, keep the most recent N.</summary>
        public void CleanupOldBackups(int keepCount = 20)
        {
            var backups = GetBackups();
            foreach (var old in backups.Skip(keepCount))
            {
                DeleteBackup(old.FilePath);
            }
        }

        /// <summary>Get the backup directory path.</summary>
        public string GetBackupDirectory() => _backupDir;
    }
}

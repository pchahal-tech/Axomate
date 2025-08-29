using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Axomate.ApplicationLayer.Services
{
    public class DbMaintenanceService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;
        private readonly string _backupDir;
        private readonly string _logPath;

        public DbMaintenanceService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? "Data Source=|DataDirectory|\\Axomate.db";

            _dbPath = ResolveDbPath(_connectionString);

            var dataDir = Path.GetDirectoryName(_dbPath) ?? AppContext.BaseDirectory;
            _backupDir = Path.Combine(dataDir, "Backups");
            Directory.CreateDirectory(_backupDir);

            _logPath = Path.Combine(dataDir,"HealthLogs", "dbHealth.log");
        }

        // ---- Public props used by UI ----
        public string DbFilePath => _dbPath;
        public string LogFilePath => _logPath;
        public string BackupsFolderPath => _backupDir;

        public string GetLastBackupDate()
        {
            var latest = Directory.GetFiles(_backupDir, "Axomate_*.db")
                                  .OrderByDescending(f => f)
                                  .FirstOrDefault();
            return latest != null ? File.GetCreationTime(latest).ToString("yyyy-MM-dd HH:mm") : "None";
        }

        // ---- Startup checks (optional to call on app start) ----
        public async Task RunStartupChecksAsync()
        {
            try
            {
                CreateBackup(); // daily
                var integrity = await RunIntegrityCheckAsync();
                if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                    await RunVacuumAsync();
                LogStatus(integrity);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logPath, $"[{DateTime.Now}] DB Maintenance failed: {ex.Message}{Environment.NewLine}");
            }
        }

        // ---- Actions exposed to Admin UI ----
        public void CreateBackupNow() => CreateBackup();
        public Task<string> RunIntegrityCheckNowAsync() => RunIntegrityCheckAsync();
        public Task RunVacuumNowAsync() => RunVacuumAsync();

        // ---- Internals ----
        private void CreateBackup()
        {
            if (!File.Exists(_dbPath)) return;

            var backupFile = Path.Combine(_backupDir, $"Axomate_{DateTime.Now:yyyy-MM-dd}.db");
            if (!File.Exists(backupFile))
                File.Copy(_dbPath, backupFile, overwrite: false);

            // keep last 15
            foreach (var old in Directory.GetFiles(_backupDir, "Axomate_*.db").OrderByDescending(f => f).Skip(15))
                File.Delete(old);
        }

        private async Task<string> RunIntegrityCheckAsync()
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            return (string?)await cmd.ExecuteScalarAsync() ?? "unknown";
        }

        private async Task RunVacuumAsync()
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "VACUUM;";
            await cmd.ExecuteNonQueryAsync();
        }

        private void LogStatus(string integrity)
        {
            var sizeMb = File.Exists(_dbPath) ? new FileInfo(_dbPath).Length / (1024 * 1024) : 0;
            var status = integrity == "ok" ? "Healthy" : $"Integrity FAIL: {integrity}";

            var fi = new FileInfo(_logPath);
            fi.Directory?.Create();

            File.AppendAllText(_logPath, $"[{DateTime.Now}] DB Size: {sizeMb} MB – {status}{Environment.NewLine}");
        }

        private static string ResolveDbPath(string connectionString)
        {
            const string token = "|DataDirectory|";
            var dataDir = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString() ?? AppContext.BaseDirectory;

            // trivial parser for "Data Source=..."
            var parts = connectionString.Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            {
                var path = parts[1].Trim();
                if (path.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                    path = Path.Combine(dataDir, path.Substring(token.Length).TrimStart('\\', '/'));
                return path;
            }

            throw new InvalidOperationException("Invalid connection string format.");
        }
    }
}

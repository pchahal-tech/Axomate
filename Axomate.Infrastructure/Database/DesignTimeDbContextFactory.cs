using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite; // <— add package Microsoft.Data.Sqlite

namespace Axomate.Infrastructure.Database
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AxomateDbContext>
    {
        public AxomateDbContext CreateDbContext(string[] args)
        {
            // Load appsettings.json from current working dir (startup project when -s is used)
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var raw = config.GetConnectionString("DefaultConnection")
                      ?? "Data Source=|DataDirectory|\\Axomate.db";

            var final = ResolveSqliteConnectionString(raw);

            var options = new DbContextOptionsBuilder<AxomateDbContext>()
                .UseSqlite(final /*, b => b.MigrationsAssembly("Axomate.Infrastructure.Database")*/ )
                .Options;

            return new AxomateDbContext(options);
        }

        private static string ResolveSqliteConnectionString(string cs)
        {
            const string token = "|DataDirectory|";
            var cwd = Directory.GetCurrentDirectory();              // usually StartupProject dir
            var dataDir = Path.Combine(cwd, "Data");
            Directory.CreateDirectory(dataDir);

            if (cs.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Replace |DataDirectory| with absolute Data folder
                return cs.Replace(token, dataDir, StringComparison.OrdinalIgnoreCase);
            }

            // If Data Source is relative, make it absolute in the current dir
            var b = new SqliteConnectionStringBuilder(cs);
            var ds = b.DataSource;

            if (!Path.IsPathRooted(ds))
            {
                // If it's just a filename, put it under Data\
                if (!ds.Contains(Path.DirectorySeparatorChar) && !ds.Contains(Path.AltDirectorySeparatorChar))
                    b.DataSource = Path.Combine(dataDir, ds);
                else
                    b.DataSource = Path.GetFullPath(Path.Combine(cwd, ds));
            }

            // Ensure directory exists
            var dir = Path.GetDirectoryName(b.DataSource);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            return b.ToString();
        }
    }
}

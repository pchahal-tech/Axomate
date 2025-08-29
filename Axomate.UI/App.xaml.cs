using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.ApplicationLayer.Services;
using Axomate.ApplicationLayer.Services.Auth;
using Axomate.ApplicationLayer.Services.Pdf;
using Axomate.Infrastructure.Database;
using Axomate.Infrastructure.Database.Repositories;
using Axomate.Infrastructure.Database.Seeders;
using Axomate.Infrastructure.Utils;
using Axomate.UI.Services;
using Axomate.UI.ViewModels;
using Axomate.UI.Views;
using Microsoft.Data.Sqlite;                 // ✅ important for controlled SQLite connection string
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Axomate.UI
{
    public partial class App : Application
    {
        private IHost? _host;
        private SplashScreenWindow? _splash;
        private string _startupLogPath = string.Empty;
        private string _errorLogPath = string.Empty;
        private Stopwatch? _stepWatch;

        public static IServiceProvider Services => ((App)Current)._host!.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ------------------------------------------------------------
            // Global exception handlers
            // ------------------------------------------------------------
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                LogFatal("AppDomain.CurrentDomain.UnhandledException", ex);
                MessageBox.Show(ex?.ToString() ?? "Unknown error",
                    "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (s, args) =>
            {
                LogFatal("Application.DispatcherUnhandledException", args.Exception);
                MessageBox.Show(args.Exception.ToString(),
                    "UI Thread Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogFatal("TaskScheduler.UnobservedTaskException", args.Exception);
                args.SetObserved();
            };

            // ------------------------------------------------------------
            // 0) Pick a writable data dir early and set |DataDirectory|
            // ------------------------------------------------------------
            var chosenDataDir = ChooseWritableDataDir();
            AppDomain.CurrentDomain.SetData("DataDirectory", chosenDataDir);

            // ------------------------------------------------------------
            // 1) Force splash to be visible and painted
            // ------------------------------------------------------------
            var oldShutdownMode = ShutdownMode;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;   // keep app alive until we set MainWindow

            _splash = new SplashScreenWindow
            {
                Topmost = true,
                ShowActivated = true,
                Visibility = Visibility.Visible,
                Opacity = 1.0
            };
            _splash.CancelRequested += async () =>
            {
                UpdateSplash("Cancelling…");
                await Task.Delay(200);
                Shutdown();
            };
            _splash.Show();
            _splash.Activate();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            LogStatus("Splash shown (pre-render)", null);

            var ver = typeof(App).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
#if DEBUG
            var envName = "Debug";
#else
            var envName = "Release";
#endif
            _splash.SetFooter($"v{ver} • {envName} • {(Environment.Is64BitProcess ? "x64" : "x86")}");

            var shownAt = DateTime.UtcNow;
            UpdateSplash("Starting…", 2);

            // ------------------------------------------------------------
            // 2) Build Host (loads config, registers services)
            // ------------------------------------------------------------
            StepStart("Build host");
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
#if DEBUG
                    config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
#else
                    config.AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true);
#endif
                })
                .ConfigureServices((context, services) =>
                {
                    var cfg = context.Configuration;

                    // Options
                    services.Configure<InvoicePdfOptions>(cfg.GetSection("InvoicePdf"));
                    services.Configure<AppLogOptions>(cfg.GetSection("Logging"));

                    // ============= DbContext with SAFE absolute connection string ============
                    var configured = cfg.GetConnectionString("DefaultConnection");
                    string normalized = string.IsNullOrWhiteSpace(configured)
                        ? $"Data Source={Path.Combine(chosenDataDir, "Axomate.db")}"
                        : Environment.ExpandEnvironmentVariables(configured)
                                      .Replace("|DataDirectory|", chosenDataDir, StringComparison.OrdinalIgnoreCase);

                    var b = new SqliteConnectionStringBuilder(normalized);

                    // Ensure absolute path
                    string ds = string.IsNullOrWhiteSpace(b.DataSource)
                        ? Path.Combine(chosenDataDir, "Axomate.db")
                        : (Path.IsPathRooted(b.DataSource) ? b.DataSource
                           : Path.GetFullPath(Path.Combine(chosenDataDir, b.DataSource)));

                    // If configured folder is unwritable (Program Files / locked ProgramData), force fallback
                    string dsDir = Path.GetDirectoryName(ds)!;
                    if (!TryEnsureWritable(dsDir) || IsSystemProtected(dsDir))
                    {
                        dsDir = chosenDataDir;
                        ds = Path.Combine(dsDir, "Axomate.db");
                    }
                    Directory.CreateDirectory(dsDir);

                    // Clear read-only on existing DB file if any
                    try
                    {
                        if (File.Exists(ds))
                        {
                            var attrs = File.GetAttributes(ds);
                            if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                File.SetAttributes(ds, attrs & ~FileAttributes.ReadOnly);
                        }
                    }
                    catch { /* ignore */ }

                    b.DataSource = ds;
                    b.Mode = SqliteOpenMode.ReadWriteCreate;
                    b.Cache = SqliteCacheMode.Shared;
                    if (!b.ContainsKey("Pooling")) b["Pooling"] = false; // optional

                    string finalConnStr = b.ToString();

                    services.AddDbContext<AxomateDbContext>(options => options.UseSqlite(finalConnStr));

                    // Repositories
                    services.AddScoped<ICustomerRepository, CustomerRepository>();
                    services.AddScoped<IVehicleRepository, VehicleRepository>();
                    services.AddScoped<ICompanyRepository, CompanyRepository>();
                    services.AddScoped<IInvoiceRepository, InvoiceRepository>();
                    services.AddScoped<IServiceItemRepository, ServiceItemRepository>();
                    services.AddScoped<IInvoiceLineItemRepository, InvoiceLineItemRepository>();
                    services.AddScoped<IMileageHistoryRepository, MileageHistoryRepository>();
                    services.AddScoped<IAdminCredentialRepository, AdminCredentialRepository>();

                    // Services
                    services.AddScoped<IInvoiceService, InvoiceService>();
                    services.AddScoped<IInvoicePdfService, InvoicePdfService>();
                    services.AddScoped<ICustomerService, CustomerService>();
                    services.AddScoped<IVehicleService, VehicleService>();
                    services.AddScoped<ICompanyService, CompanyService>();
                    services.AddScoped<IServiceItemService, ServiceItemService>();
                    services.AddScoped<IMileageHistoryService, MileageHistoryService>();
                    services.AddScoped<IAuthService, AuthService>();
                    services.AddScoped<ICurrentUserService, CurrentUserService>();

                    // ViewModels
                    services.AddScoped<MainViewModel>();
                    services.AddScoped<InvoiceViewModel>();
                    services.AddScoped<CustomerViewModel>();
                    services.AddScoped<VehicleViewModel>();
                    services.AddTransient<AdminViewModel>();

                    // Views
                    services.AddScoped<MainWindow>();
                    services.AddTransient<Axomate.UI.Views.AdminWindow>();

                    // Seeders (prod-safe)
                    services.AddTransient<ServiceItemSeeder>();
                    services.AddTransient<CompanySeeder>();
#if DEBUG
                    // Sample seeder is DEBUG-only (not even compiled for Release)
                    services.AddTransient<SampleDataSeeder>();
#endif

                    // Infra
                    services.AddSingleton<DbMaintenanceService>();
                    services.AddSingleton<IWindowService, WindowService>();
                })
                .Build();
            StepEnd("Build host");

            try
            {
                // ------------------------------------------------------------
                // 3) Ensure filesystem (Logs / Invoices / Data) BEFORE any writes
                // ------------------------------------------------------------
                StepStart("Ensure filesystem");
                using (var scope = _host.Services.CreateScope())
                {
                    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var logOpts = scope.ServiceProvider.GetRequiredService<IOptions<AppLogOptions>>().Value;

                    // Base dirs (ProgramData for Logs/Invoices, DB dir already chosen)
                    var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    var baseDir = Path.Combine(programData, "Axomate");
                    var dataDir = (string)(AppDomain.CurrentDomain.GetData("DataDirectory") ?? chosenDataDir);
                    var logsDir = Path.Combine(baseDir, "Logs");
                    var invoicesDir = Environment.ExpandEnvironmentVariables(
                        cfg["InvoicePdf:OutputDirectory"] ?? Path.Combine(baseDir, "Invoices"));

                    Directory.CreateDirectory(baseDir);
                    Directory.CreateDirectory(dataDir);
                    Directory.CreateDirectory(logsDir);
                    Directory.CreateDirectory(invoicesDir);

                    _startupLogPath = Environment.ExpandEnvironmentVariables(logOpts.StartupLogPath);
                    _errorLogPath = Environment.ExpandEnvironmentVariables(logOpts.ErrorLogPath);

                    Directory.CreateDirectory(Path.GetDirectoryName(_startupLogPath)!);
                    Directory.CreateDirectory(Path.GetDirectoryName(_errorLogPath)!);

                    AppDomain.CurrentDomain.SetData("DataDirectory", dataDir);
                }
                StepEnd("Ensure filesystem");

                // ------------------------------------------------------------
                // 4) Migrate + verify, then backfills / auth init / (DEBUG-only) sample seeding
                // ------------------------------------------------------------
                _splash.SetIndeterminate(true);
                StepStart("Migrate database / initialize data");
                await EnsureDatabaseReadyAsync(_host.Services);

                using (var scope = _host.Services.CreateScope())
                {
                    var sp = scope.ServiceProvider;
                    var config = sp.GetRequiredService<IConfiguration>();

                    UpdateSplash("Running backfills…", 55);
                    var db = sp.GetRequiredService<AxomateDbContext>();
                    await SecuritySidecarBackfill.RunAsync(db);
                    await CompanyGstBackfill.RunAsync(db);

                    var auth = sp.GetRequiredService<IAuthService>();
                    await auth.EnsureInitializedAsync();

                    // ---- SAMPLE SEEDING: DISABLED IN RELEASE ----
#if DEBUG
                    // Default ON in Debug unless overridden in config
                    bool seedData = config.GetValue<bool?>("Startup:SeedSampleData") ?? true;
                    if (seedData)
                    {
                        UpdateSplash("Seeding sample data…", 70);
                        var seeder = sp.GetRequiredService<SampleDataSeeder>();
                        await seeder.SeedAsync();
                    }
#else
                    // In Release, default OFF (and SampleDataSeeder isn't even registered)
                    bool seedData = config.GetValue<bool?>("Startup:SeedSampleData") ?? false;
#endif
                    // --------------------------------------------

#if DEBUG
                    int minSplashMsDefault = 0;
#else
                    int minSplashMsDefault = 600;
#endif
                    int minSplashMs = config.GetValue<int?>("Startup:MinSplashMs") ?? minSplashMsDefault;
                    var elapsed = DateTime.UtcNow - shownAt;
                    var minimum = TimeSpan.FromMilliseconds(minSplashMs);
                    if (elapsed < minimum) await Task.Delay(minimum - elapsed);
                }
                StepEnd("Migrate database / initialize data");
                _splash.SetIndeterminate(false);

                // ------------------------------------------------------------
                // 5) Start host
                // ------------------------------------------------------------
                _splash.SetIndeterminate(true);
                StepStart("Start host");
                await _host.StartAsync();
                StepEnd("Start host");
                _splash.SetIndeterminate(false);

                // ------------------------------------------------------------
                // 6) Maintenance / health check
                // ------------------------------------------------------------
                var dbm = _host.Services.GetRequiredService<DbMaintenanceService>();
                await dbm.RunStartupChecksAsync();

                var cs2 = _host.Services.GetRequiredService<AxomateDbContext>().Database.GetDbConnection().ConnectionString;
                var ds2 = new SqliteConnectionStringBuilder(cs2).DataSource;
                if (DatabaseHealthChecker.IsDatabaseTooLarge(ds2, out long sizeBytes2))
                {
                    MessageBox.Show(
                        $"⚠️ Your Axomate database has grown to {sizeBytes2 / (1024 * 1024)} MB.\n\n" +
                        "For best performance, consider archiving old invoices or cleaning unused data.",
                        "Database Size Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }

                // ------------------------------------------------------------
                // 7) Show main window and close splash
                // ------------------------------------------------------------
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
                MainWindow = mainWindow;

                // restore default shutdown behavior now that MainWindow is set
                ShutdownMode = oldShutdownMode;

                if (_splash != null)
                    await FadeOutAndCloseAsync(_splash, 250);

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                try
                {
                    UpdateSplash("Startup failed. See details…");
                    await Task.Delay(600);
                    _splash?.Close();
                }
                catch { }

                MessageBox.Show($"Startup Error:\n\n{ex}", "Startup Failure",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        // ============================================================
        // Writable data-dir selection & checks
        // ============================================================
        private static string ChooseWritableDataDir()
        {
            string programData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Axomate", "Data");
            if (TryEnsureWritable(programData)) return programData;

            string local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Axomate", "Data");
            if (TryEnsureWritable(local)) return local;

            string temp = Path.Combine(Path.GetTempPath(), "Axomate", "Data");
            Directory.CreateDirectory(temp);
            return temp;
        }

        private static bool TryEnsureWritable(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string test = Path.Combine(dir, ".write_test");
                File.WriteAllText(test, "ok");
                File.Delete(test);
                return true;
            }
            catch { return false; }
        }

        private static bool IsSystemProtected(string dir)
        {
            var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            bool under(string root) => !string.IsNullOrEmpty(root) &&
                                       dir.StartsWith(root, StringComparison.OrdinalIgnoreCase);

            return under(pf) || under(pf86) || under(windows);
        }

        // ============================================================
        // DB bring-up & verification
        // ============================================================
        private async Task EnsureDatabaseReadyAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var cfg = sp.GetRequiredService<IConfiguration>();
            var db = sp.GetRequiredService<AxomateDbContext>();

            // Get actual connection string/path EF will use
            var connStr = db.Database.GetDbConnection().ConnectionString;
            var builder = new SqliteConnectionStringBuilder(connStr);
            var resolvedPath = builder.DataSource;

            // Ensure parent dir exists & writable; clear read-only on existing file
            var dbDir = Path.GetDirectoryName(resolvedPath)!;
            Directory.CreateDirectory(dbDir);
            if (!TryEnsureWritable(dbDir))
            {
                var msg = $"Database directory is not writable:\n{dbDir}\n\n" +
                          $"Run as a user with write access or reinstall with permissive ACLs.";
                MessageBox.Show(msg, "Axomate – DB Folder Readonly", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new IOException(msg);
            }
            if (File.Exists(resolvedPath))
            {
                try
                {
                    var attrs = File.GetAttributes(resolvedPath);
                    if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        File.SetAttributes(resolvedPath, attrs & ~FileAttributes.ReadOnly);
                }
                catch { }
            }

            LogStatus($"DB path resolved to: {resolvedPath}", null);

            // Apply migrations (or create if disabled)
            bool runMigrations = cfg.GetValue<bool?>("Startup:RunMigrations") ?? true;
            try
            {
                UpdateSplash("Applying database migrations…", 25);
                if (runMigrations)
                    await db.Database.MigrateAsync();
                else
                    await db.Database.EnsureCreatedAsync();
                LogStatus("Migrations/creation completed.", null);
            }
            catch (Exception ex)
            {
                LogFatal("Migrate/EnsureCreated failed", ex);
                throw;
            }

            // Verify tables exist
            var required = new[]
            {
                "Companies","Customers","Vehicles","Invoices","InvoiceLineItems",
                "ServiceItems","MileageHistories","AdminCredentials","__EFMigrationsHistory"
            };

            var existing = await ListSqliteTablesAsync(db);
            var missing = required.Where(t => !existing.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();

            LogStatus($"Existing tables: {string.Join(", ", existing)}", null);

            if (missing.Count > 0)
            {
                var msg =
                    "Database verification failed.\n" +
                    $"DB file: {resolvedPath}\n" +
                    $"Missing tables: {string.Join(", ", missing)}\n\n" +
                    "Likely causes: a different (readonly) DB path from MSI install, or ACLs preventing write. " +
                    "The app now forces a writable path automatically—if you still see this, delete stale DBs and re-run.";
                MessageBox.Show(msg, "Axomate – Missing Tables", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new InvalidOperationException(msg);
            }
        }

        private static async Task<HashSet<string>> ListSqliteTablesAsync(DbContext db)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var conn = db.Database.GetDbConnection();

            try
            {
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    if (!rdr.IsDBNull(0))
                        set.Add(rdr.GetString(0));
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }

            return set;
        }

        // ============================================================
        // Splash / logging utilities
        // ============================================================
        private void UpdateSplash(string message, double? progressPercent = null)
        {
            _splash?.SetStatus(message);
            if (progressPercent.HasValue) _splash?.SetProgress(progressPercent.Value);
            LogStatus(message, progressPercent);
        }

        private void LogStatus(string message, double? progressPercent)
        {
            try
            {
                var line = $"{DateTime.Now:O}  {(progressPercent.HasValue ? $"[{progressPercent:0}%] " : "")}{message}{Environment.NewLine}";
                File.AppendAllText(_startupLogPath, line);
            }
            catch { }
        }

        private void StepStart(string name)
        {
            _stepWatch = Stopwatch.StartNew();
            LogStatus($"→ {name}...", null);
        }

        private void StepEnd(string name)
        {
            if (_stepWatch == null) return;
            _stepWatch.Stop();
            LogStatus($"✓ {name} in {_stepWatch.Elapsed.TotalMilliseconds:0} ms", null);
            _stepWatch = null;
        }

        private static Task FadeOutAndCloseAsync(Window window, int ms)
        {
            var tcs = new TaskCompletionSource<bool>();
            var anim = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(ms)),
                FillBehavior = FillBehavior.Stop
            };
            anim.Completed += (_, __) =>
            {
                window.Opacity = 0;
                window.Close();
                tcs.TrySetResult(true);
            };
            window.BeginAnimation(UIElement.OpacityProperty, anim);
            return tcs.Task;
        }

        private void LogFatal(string source, Exception? ex)
        {
            try
            {
                var line = $"{DateTime.Now:O} [FATAL] {source}: {ex}{Environment.NewLine}";
                File.AppendAllText(_errorLogPath, line);
            }
            catch { }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_host is not null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                    _host = null;
                }
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }

    public class AppLogOptions
    {
        public string StartupLogPath { get; set; } = "startup.log";
        public string ErrorLogPath { get; set; } = "error.log";
    }
}

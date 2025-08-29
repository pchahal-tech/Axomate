// Axomate.UI/ViewModels/AdminViewModel.cs
using Axomate.ApplicationLayer.Interfaces.Services;      // ICompanyService
using Axomate.ApplicationLayer.Services;                 // DbMaintenanceService
using Axomate.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace Axomate.UI.ViewModels
{
    public partial class AdminViewModel : ObservableObject
    {
        private readonly DbMaintenanceService _db;
        private readonly ICompanyService _companyService;
        private readonly IConfiguration _config;

        private Company? _company;

        // ===== DB info (binds to Overview/Database tabs) =====
        [ObservableProperty] private string dbPath = string.Empty;
        [ObservableProperty] private string dbSize = "0";
        [ObservableProperty] private string lastBackup = "None";
        [ObservableProperty] private string integrityStatus = "Unknown";

        // ===== Company info (binds to Company tab) =====
        [ObservableProperty] private string companyName = string.Empty;
        [ObservableProperty] private string companyTagline = string.Empty;
        [ObservableProperty] private string companyAddressLine1 = string.Empty;
        [ObservableProperty] private string companyAddressLine2 = string.Empty;
        [ObservableProperty] private string companyPhone1 = string.Empty;
        [ObservableProperty] private string companyPhone2 = string.Empty;
        [ObservableProperty] private string companyEmail = string.Empty;
        [ObservableProperty] private string companyWebsite = string.Empty;
        [ObservableProperty] private string companyLogoFileName = string.Empty; // prefer filename (not full path)
        [ObservableProperty] private string companyGstNumber = string.Empty;

        public AdminViewModel(
            DbMaintenanceService db,
            ICompanyService companyService,
            IConfiguration config)
        {
            _db = db;
            _companyService = companyService;
            _config = config;

            LoadDbInfo();
            _ = LoadCompanyAsync();
        }

        // ---------- DB helpers ----------
        private void LoadDbInfo()
        {
            DbPath = _db.DbFilePath;
            var bytes = File.Exists(DbPath) ? new FileInfo(DbPath).Length : 0L;
            DbSize = (bytes / (1024 * 1024)).ToString();
            LastBackup = _db.GetLastBackupDate();
            // IntegrityStatus updated via command; default "Unknown"
        }

        // ---------- Company load (Option C: config-driven defaults if DB empty) ----------
        private async Task LoadCompanyAsync()
        {
            _company = await _companyService.GetAsync();

            if (_company == null)
            {
                // Build a transient Company from config (not saved until user clicks Save)
                var defaults = new Company();
                _config.GetSection("CompanyDefaults").Bind(defaults);

                // Handle LogoFileName vs LogoPath
                var logoName = _config["CompanyDefaults:LogoFileName"];
                var logoProp = defaults.GetType().GetProperty("LogoFileName");
                if (logoProp != null && !string.IsNullOrWhiteSpace(logoName))
                    logoProp.SetValue(defaults, logoName);

                _company = defaults;
            }

            // Entity -> VM
            CompanyName = _company.Name ?? string.Empty;
            CompanyTagline = _company.Tagline ?? string.Empty;
            CompanyAddressLine1 = _company.AddressLine1 ?? string.Empty;
            CompanyAddressLine2 = _company.AddressLine2 ?? string.Empty;
            CompanyPhone1 = _company.Phone1 ?? string.Empty;
            CompanyPhone2 = _company.Phone2 ?? string.Empty;
            CompanyEmail = _company.Email ?? string.Empty;
            CompanyWebsite = _company.Website ?? string.Empty;

            var prop = _company.GetType().GetProperty("LogoFileName");
            CompanyLogoFileName = prop != null
                ? (prop.GetValue(_company) as string) ?? string.Empty
                : (_company.LogoPath ?? string.Empty);

            CompanyGstNumber = _company.GstNumber ?? string.Empty;
        }

        // ---------- DB commands ----------
        [RelayCommand]
        private async Task RunIntegrityCheckAsync()
        {
            IntegrityStatus = await _db.RunIntegrityCheckNowAsync();
            LoadDbInfo();
        }

        [RelayCommand]
        private async Task VacuumAsync()
        {
            await _db.RunVacuumNowAsync();
            LoadDbInfo();
        }

        [RelayCommand]
        private void Backup()
        {
            _db.CreateBackupNow();
            LoadDbInfo();
        }

        [RelayCommand]
        private void ViewLog()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _db.LogFilePath,
                UseShellExecute = true
            });
        }

        [RelayCommand]
        private void OpenDataFolder()
        {
            var dir = Path.GetDirectoryName(_db.DbFilePath) ?? ".";
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }

        [RelayCommand]
        private void OpenBackupsFolder()
        {
            var dir = _db.BackupsFolderPath;
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }

        // ---------- Company commands ----------
        [RelayCommand]
        private async Task SaveCompanyAsync()
        {
            if (_company == null) _company = new Company();

            _company.Name = CompanyName?.Trim() ?? string.Empty;
            _company.Tagline = string.IsNullOrWhiteSpace(CompanyTagline) ? null : CompanyTagline.Trim();
            _company.AddressLine1 = string.IsNullOrWhiteSpace(CompanyAddressLine1) ? null : CompanyAddressLine1.Trim();
            _company.AddressLine2 = string.IsNullOrWhiteSpace(CompanyAddressLine2) ? null : CompanyAddressLine2.Trim();
            _company.Phone1 = string.IsNullOrWhiteSpace(CompanyPhone1) ? null : CompanyPhone1.Trim();
            _company.Phone2 = string.IsNullOrWhiteSpace(CompanyPhone2) ? null : CompanyPhone2.Trim();
            _company.Email = string.IsNullOrWhiteSpace(CompanyEmail) ? null : CompanyEmail.Trim();
            _company.Website = string.IsNullOrWhiteSpace(CompanyWebsite) ? null : CompanyWebsite.Trim();

            var logoProp = _company.GetType().GetProperty("LogoFileName");
            if (logoProp != null)
                logoProp.SetValue(_company, string.IsNullOrWhiteSpace(CompanyLogoFileName) ? null : CompanyLogoFileName.Trim());
            else
                _company.LogoPath = string.IsNullOrWhiteSpace(CompanyLogoFileName) ? null : CompanyLogoFileName.Trim();

            _company.GstNumber = string.IsNullOrWhiteSpace(CompanyGstNumber) ? null : CompanyGstNumber.Trim();

            await _companyService.UpdateAsync(_company);
            await LoadCompanyAsync();
        }

        [RelayCommand]
        private async Task ResetCompanyAsync() => await LoadCompanyAsync();

        [RelayCommand]
        private void BrowseLogo()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Logo",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                CompanyLogoFileName = Path.GetFileName(dlg.FileName); // store filename only
            }
        }
    }
}

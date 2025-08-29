using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;
using Axomate.UI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Axomate.UI.ViewModels
{
    // Row wrapper for the service items grid so price edits raise PropertyChanged
    public partial class ServiceItemRow : ObservableObject
    {
        public int? Id { get; init; }

        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private decimal price;
        [ObservableProperty] private int quantity = 1;

        partial void OnQuantityChanged(int value)
        {
            if (value < 1) Quantity = 1;
        }

        partial void OnPriceChanged(decimal value)
        {
            if (value < 0m) Price = 0m;
        }
    }

    // UI-only row to show vehicle service history
    public partial class ServiceHistoryRow : ObservableObject
    {
        public int InvoiceId { get; init; }
        public DateTime ServiceDate { get; init; }
        public int? Mileage { get; init; }
        public decimal Total { get; init; }
        public string ItemsSummary { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string VehicleDisplay { get; init; } = string.Empty;
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly ICustomerService _customerService;
        private readonly IVehicleService _vehicleService;
        private readonly IServiceItemService _serviceItemService;
        private readonly ICompanyService _companyService;
        private readonly IInvoiceService _invoiceService;
        private readonly IInvoicePdfService _invoicePdfService;
        private readonly IMileageHistoryService _mileageHistoryService;
        private readonly IAuthService _authService;

        // ---- UI state ----
        [ObservableProperty] private bool isAdminUnlocked;

        [ObservableProperty] private Customer newCustomer = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewVehicleCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddNewCustomerCommand))]
        private Customer? selectedCustomer;

        [ObservableProperty] private Vehicle? vehicle = new();

        [ObservableProperty] private DateTime serviceDate = DateTime.Now;

        [ObservableProperty] private ObservableCollection<Customer> customers = new();
        [ObservableProperty] private ObservableCollection<ServiceItem> serviceItems = new();
        [ObservableProperty] private ObservableCollection<ServiceItemRow> selectedServices = new();
        [ObservableProperty] private ObservableCollection<Vehicle> vehicles = new();
        [ObservableProperty] private ObservableCollection<Vehicle> vehiclesByCustomer = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewVehicleCommand))]
        private Vehicle? selectedVehicle;

        [ObservableProperty] private Company? company;
        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private decimal total;
        [ObservableProperty] private int? mileage;
        [ObservableProperty] private DateTime? historyFromDate;
        [ObservableProperty] private DateTime? historyToDate;

        // --- History selectors for the History tab ---
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadServiceHistoryByCustomerCommand))]
        private Customer? selectedHistoryCustomer;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadServiceHistoryByVehicleCommand))]
        private Vehicle? selectedHistoryVehicle;

        [ObservableProperty]
        private ObservableCollection<Vehicle> historyVehiclesByCustomer = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveNewVehicleCommand))]
        private bool newVehicleMode;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveNewCustomerCommand))]
        private bool newCustomerMode;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isAddingVehicle;

        // Optional: UI marker; doesn't gate Save&Print
        [ObservableProperty]
        private bool newInvoiceMode;

        // Duplicate-submit lock for Save & Print (also used to keep some buttons disabled after submit)
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveAndPrintInvoiceCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddNewCustomerCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddNewVehicleCommand))]
        private bool hasSubmittedCurrentInvoice;

        // Gate the "New Invoice" button (disabled on app open; enabled only after save)
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateNewInvoiceCommand))]
        private bool newInvoiceEnabled = false;

        // ---- Service History (UI) ----
        [ObservableProperty] private ObservableCollection<ServiceHistoryRow> serviceHistory = new();

        // VM field for raw input (no converters/validators needed)
        [ObservableProperty] private string? mileageText;

        private static string FormatVehicleDisplay(Vehicle? v)
        {
            if (v is null) return string.Empty;
            var parts = new List<string>();
            if (v.Year > 0) parts.Add(v.Year.ToString());
            if (!string.IsNullOrWhiteSpace(v.Make)) parts.Add(v.Make);
            if (!string.IsNullOrWhiteSpace(v.Model)) parts.Add(v.Model);
            var left = string.Join(" ", parts);
            var plate = string.IsNullOrWhiteSpace(v.LicensePlate) ? "" : $" [{v.LicensePlate}]";
            return (left + plate).Trim();
        }

        // Helper to get nullable int when needed (Mileage is source of truth; MileageText fallback)
        private int? GetMileage()
        {
            if (Mileage.HasValue && Mileage.Value > 0) return Mileage.Value;

            if (!string.IsNullOrWhiteSpace(MileageText))
            {
                var digits = new string(MileageText.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out var m) && m > 0) return m;
            }
            return null;
        }

        // backing field for SelectedServiceItem ComboBox
        private ServiceItem? selectedServiceItem;
        public ServiceItem? SelectedServiceItem
        {
            get => selectedServiceItem;
            set
            {
                if (!SetProperty(ref selectedServiceItem, value) || value is null) return;

                var isOther = string.Equals(value.Name, "Other", StringComparison.OrdinalIgnoreCase);

                // Prevent duplicates for catalog items (everything except "Other")
                if (!isOther && SelectedServices.Any(r => r.Id == value.Id))
                {
                    SetProperty(ref selectedServiceItem, null, nameof(SelectedServiceItem));
                    return;
                }

                if (isOther)
                    SelectedServices.Add(new ServiceItemRow { Name = "", Price = 0m });
                else
                    SelectedServices.Add(new ServiceItemRow { Id = value.Id, Name = value.Name, Price = value.Price });

                RecalculateTotal();
                OnPropertyChanged(nameof(AvailableServiceItems));

                // Clear without re-entering logic
                SetProperty(ref selectedServiceItem, null, nameof(SelectedServiceItem));
            }
        }

        // Alias for older XAML bindings
        public ObservableCollection<ServiceItemRow> InvoiceLineItems => SelectedServices;

        public decimal InvoiceTotal
        {
            get => Total;
            set
            {
                if (Total != value)
                {
                    Total = value;
                    OnPropertyChanged(nameof(InvoiceTotal));
                }
            }
        }

        private DateTime ServiceMomentNow => ServiceDate.Date + DateTime.Now.TimeOfDay;

        partial void OnTotalChanged(decimal value) => OnPropertyChanged(nameof(InvoiceTotal));

        private bool _suppressMileageRecording;
        private int? _lastRecordedMileage;
        private DateTime? _lastRecordedAt;
        private static readonly TimeSpan MileageEditLock = TimeSpan.FromHours(5);

        // avoid clearing invoice lines when we change SelectedCustomer inside Save&Print
        private bool _suppressCustomerChangeSideEffects;

        public MainViewModel(
            ICustomerService customerService,
            IVehicleService vehicleService,
            IServiceItemService serviceItemService,
            ICompanyService companyService,
            IInvoiceService invoiceService,
            IMileageHistoryService mileageHistoryService,
            IInvoicePdfService invoicePdfService,
            IAuthService authService)
        {
            _customerService = customerService;
            _vehicleService = vehicleService;
            _serviceItemService = serviceItemService;
            _companyService = companyService;
            _invoiceService = invoiceService;
            _mileageHistoryService = mileageHistoryService;
            _invoicePdfService = invoicePdfService;
            _authService = authService;

            SelectedServices.CollectionChanged += SelectedServices_CollectionChanged;

            _ = LoadDataAsync();
        }

        // Filtered list for the ComboBox: hides items already selected (except "Other")
        public IEnumerable<ServiceItem> AvailableServiceItems =>
            ServiceItems?.Where(s =>
                string.Equals(s.Name, "Other", StringComparison.OrdinalIgnoreCase) ||
                !SelectedServices.Any(x => x.Id == s.Id))
            ?? Enumerable.Empty<ServiceItem>();

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            var customerList = await _customerService.GetAllAsync();
            Customers = new ObservableCollection<Customer>(customerList);

            var serviceList = await _serviceItemService.GetAllAsync();
            ServiceItems = new ObservableCollection<ServiceItem>(serviceList);

            var allVehicles = customerList.SelectMany(c => c.Vehicles ?? new List<Vehicle>());
            Vehicles = new ObservableCollection<Vehicle>(allVehicles);

            Company = await _companyService.GetAsync();

            OnPropertyChanged(nameof(AvailableServiceItems));

            IsLoading = false;
        }

        private void SelectedServices_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (ServiceItemRow row in e.NewItems)
                    row.PropertyChanged += ServiceItemRow_PropertyChanged;

            if (e.OldItems != null)
                foreach (ServiceItemRow row in e.OldItems)
                    row.PropertyChanged -= ServiceItemRow_PropertyChanged;

            RecalculateTotal();
            OnPropertyChanged(nameof(AvailableServiceItems));
        }

        private void ServiceItemRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ServiceItemRow.Price) ||
                e.PropertyName == nameof(ServiceItemRow.Name) ||
                e.PropertyName == nameof(ServiceItemRow.Quantity))
            {
                RecalculateTotal();
            }
        }

        private void RecalculateTotal() => Total = SelectedServices?.Sum(s => s.Price * s.Quantity) ?? 0m;

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            SelectedVehicle = null;
            Mileage = null; MileageText = string.Empty;

            if (!_suppressCustomerChangeSideEffects)
                SelectedServices.Clear();

            VehiclesByCustomer.Clear();
            if (value != null)
            {
                NewCustomer = new Customer
                {
                    Id = value.Id,
                    Name = value.Name,
                    Phone = value.Phone,
                    Email = value.Email,
                    AddressLine1 = value.AddressLine1
                };

                _ = LoadVehiclesForCustomerAsync(value.Id); // async fetch
            }

            OnPropertyChanged(nameof(AvailableServiceItems));
            SaveNewVehicleCommand?.NotifyCanExecuteChanged();
            AddNewCustomerCommand?.NotifyCanExecuteChanged();
        }

        private async Task LoadVehiclesForCustomerAsync(int customerId)
        {
            try
            {
                IsBusy = true;
                var list = await _vehicleService.GetByCustomerIdAsync(customerId);
                VehiclesByCustomer = new ObservableCollection<Vehicle>(list ?? new List<Vehicle>());
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static bool PriceEquals2Dp(decimal a, decimal b)
            => Math.Abs(Math.Round(a, 2) - Math.Round(b, 2)) < 0.005m;

        private async Task MapCustomItemsToCatalogAsync(List<InvoiceLineItem> items)
        {
            // Unique (nameLower, price2dp) for custom rows (no ServiceItemId)
            var customs = items
                .Where(li => (!li.ServiceItemId.HasValue || li.ServiceItemId.Value <= 0)
                             && !string.IsNullOrWhiteSpace(li.Description))
                .GroupBy(li => (
                    NameLower: li.Description!.Trim().ToLowerInvariant(),
                    Price2dp: Math.Round(li.Price, 2)
                ))
                .Select(g => g.Key)
                .ToList();

            foreach (var (nameLower, price2dp) in customs)
            {
                // Recover original-cased name from one of the matching items
                var nameOriginal = items
                    .Where(li => (!li.ServiceItemId.HasValue || li.ServiceItemId.Value <= 0)
                                 && !string.IsNullOrWhiteSpace(li.Description)
                                 && li.Description!.Trim().ToLowerInvariant() == nameLower
                                 && PriceEquals2Dp(li.Price, price2dp))
                    .Select(li => li.Description!.Trim())
                    .First();

                // Search by name, then exact-match price (2dp)
                var matches = await _serviceItemService.SearchByNameAsync(nameOriginal);
                var exact = matches?.FirstOrDefault(s =>
                    s.Name.Equals(nameOriginal, StringComparison.OrdinalIgnoreCase) &&
                    PriceEquals2Dp(s.Price, price2dp));

                int id;
                if (exact != null)
                {
                    id = exact.Id;
                }
                else
                {
                    // Create new catalog item (name + price)
                    var newItem = new ServiceItem { Name = nameOriginal, Price = price2dp };
                    id = await _serviceItemService.AddAsync(newItem);

                    try
                    {
                        ServiceItems.Add(new ServiceItem { Id = id, Name = newItem.Name, Price = newItem.Price });
                        OnPropertyChanged(nameof(AvailableServiceItems));
                    }
                    catch { /* non-fatal UI update */ }
                }

                // Wire the found/created Id back to all matching invoice lines
                foreach (var li in items.Where(li =>
                             (!li.ServiceItemId.HasValue || li.ServiceItemId.Value <= 0)
                             && !string.IsNullOrWhiteSpace(li.Description)
                             && li.Description!.Trim().ToLowerInvariant() == nameLower
                             && PriceEquals2Dp(li.Price, price2dp)))
                {
                    li.ServiceItemId = id;
                }
            }
        }

        // --- Duplicate-service confirmation helpers ---
        private static string LineKey(InvoiceLineItem li)
        {
            if (li.ServiceItemId.HasValue && li.ServiceItemId.Value > 0)
                return $"ID:{li.ServiceItemId.Value}";

            var nameLower = (li.Description ?? string.Empty).Trim().ToLowerInvariant();
            var price2dp = Math.Round(li.Price, 2).ToString("0.00");
            return $"DESC:{nameLower}|P:{price2dp}";
        }

        private async Task<bool> ConfirmDuplicateServicesAsync(int vehicleId, List<InvoiceLineItem> items, DateTime when)
        {
            if (vehicleId <= 0 || items is null || items.Count == 0)
                return true;

            var since = when.Date.AddDays(-5);

            // Pull recent invoices (last 5 days) for this vehicle with details
            var recent = await _invoiceService.GetByVehicleAsync(vehicleId, includeDetails: true);
            var recentFiltered = recent
                .Where(inv => inv.ServiceDate.Date >= since && inv.ServiceDate.Date <= when.Date)
                .ToList();

            if (recentFiltered.Count == 0) return true;

            // Keys for current invoice items (after mapping to catalog IDs)
            var currentKeys = new HashSet<string>(items.Select(LineKey));

            // Find overlaps
            var overlaps = new List<(string Name, DateTime Date, int InvoiceId)>();
            foreach (var inv in recentFiltered)
            {
                foreach (var li in inv.LineItems ?? Enumerable.Empty<InvoiceLineItem>())
                {
                    if (currentKeys.Contains(LineKey(li)))
                    {
                        var name = li.ServiceItem?.Name ?? li.Description ?? "Service";
                        overlaps.Add((name, inv.ServiceDate.Date, inv.Id));
                    }
                }
            }

            if (overlaps.Count == 0) return true;

            // Build confirmation message
            var lines = overlaps
                .OrderByDescending(x => x.Date)
                .Take(6)
                .Select(x => $"• {x.Name} — {x.Date:yyyy-MM-dd} (Inv #{x.InvoiceId})");

            var msg =
                "This vehicle had the same service within the last 5 days:\n\n" +
                string.Join("\n", lines) +
                "\n\nDo you want to save anyway?";

            var result = MessageBox.Show(msg, "Confirm recent duplicate service",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
        // --- END helpers ---

        // NEW: tolerant form input check for vehicle
        private static bool HasVehicleInput(Vehicle? v)
        {
            if (v is null) return false;
            static bool Has(string? s) => !string.IsNullOrWhiteSpace(s?.Trim());
            return Has(v.VIN) || Has(v.LicensePlate) || (Has(v.Make) && Has(v.Model));
        }

        partial void OnSelectedVehicleChanged(Vehicle? value)
        {
            if (value != null)
            {
                Vehicle = new Vehicle
                {
                    Id = value.Id,
                    Make = value.Make,
                    Model = value.Model,
                    Year = value.Year,
                    LicensePlate = value.LicensePlate,
                    VIN = value.VIN,
                    Color = value.Color,
                    Engine = value.Engine,
                    Transmission = value.Transmission,
                    FuelType = value.FuelType
                };

                _ = PrefillMileageFromHistoryAsync(ServiceMomentNow);
                // we load history via explicit buttons on the History tab now
            }
            else
            {
                Vehicle = null;
                Mileage = null; MileageText = string.Empty;
                ServiceHistory.Clear();
            }

            AddNewVehicleCommand?.NotifyCanExecuteChanged();
        }

        partial void OnServiceDateChanged(DateTime value)
        {
            if (SelectedVehicle?.Id > 0)
                _ = PrefillMileageFromHistoryAsync(value);
        }

        partial void OnMileageChanged(int? value)
        {
            if (_suppressMileageRecording) return;
            _ = TryRecordMileageChangeAsync(value);
        }

        // keep Mileage and MileageText in sync; trigger on-change 5h rule
        partial void OnMileageTextChanged(string? value)
        {
            if (_suppressMileageRecording) return;

            if (int.TryParse(value, out var m) && m > 0)
            {
                _suppressMileageRecording = true;
                try { Mileage = m; }
                finally { _suppressMileageRecording = false; }

                _ = TryRecordMileageChangeAsync(m);
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                _suppressMileageRecording = true;
                try { Mileage = null; }
                finally { _suppressMileageRecording = false; }
            }
        }

        private async Task TryRecordMileageChangeAsync(int? value)
        {
            if (_suppressMileageRecording) return;
            if (SelectedVehicle is null || SelectedVehicle.Id <= 0) return;
            if (!value.HasValue || value.Value <= 0) return;

            var now = DateTime.Now;

            // --- 5-hour lock ---
            var lastAt = await _mileageHistoryService.GetLastRecordTimeAsync(SelectedVehicle.Id, now);
            if (lastAt.HasValue)
            {
                var delta = now - lastAt.Value;
                if (delta < MileageEditLock)
                {
                    var remaining = MileageEditLock - delta;

                    var latestPersisted = await _mileageHistoryService.GetLatestOnOrBeforeAsync(SelectedVehicle.Id, now);

                    _suppressMileageRecording = true;
                    try
                    {
                        if (latestPersisted.HasValue)
                        {
                            Mileage = latestPersisted.Value;
                            MileageText = latestPersisted.Value.ToString();
                        }
                    }
                    finally { _suppressMileageRecording = false; }

                    MessageBox.Show(
                        $"Mileage was last recorded at {lastAt.Value:yyyy-MM-dd HH:mm}. " +
                        $"You can record a new mileage in {remaining.Hours}h {remaining.Minutes}m.",
                        "Mileage Locked", MessageBoxButton.OK, MessageBoxImage.Information);

                    return;
                }
            }

            // Prevent regression
            var latestAny = await _mileageHistoryService.GetLatestOnOrBeforeAsync(SelectedVehicle.Id, now);
            if (latestAny.HasValue && value.Value < latestAny.Value) return;

            // Append a new row with full timestamp
            await _mileageHistoryService.RecordAsync(
                SelectedVehicle.Id, value.Value, now, "Manual", "Edited in Invoice form");

            _lastRecordedMileage = value.Value;
            _lastRecordedAt = now;
        }

        private CancellationTokenSource? _mileagePrefillCts;

        private async Task PrefillMileageFromHistoryAsync(DateTime reference)
        {
            if (Mileage.HasValue && Mileage.Value > 0) return;

            var v = SelectedVehicle;
            if (v?.Id <= 0) return;

            _mileagePrefillCts?.Cancel();
            var cts = _mileagePrefillCts = new CancellationTokenSource();

            try
            {
                _suppressMileageRecording = true;  // prevent recording during prefill

                var latest = await _mileageHistoryService.GetLatestForDayAsync(v.Id, reference);
                if (cts.IsCancellationRequested) return;

                Mileage = latest;
                MileageText = latest?.ToString();
            }
            finally
            {
                _suppressMileageRecording = false;
            }
        }

        private async Task RefreshLookupsAsync()
        {
            var customerList = await _customerService.GetAllAsync();
            Customers = new ObservableCollection<Customer>(customerList);

            var serviceList = await _serviceItemService.GetAllAsync();
            ServiceItems = new ObservableCollection<ServiceItem>(serviceList);

            var allVehicles = customerList.SelectMany(c => c.Vehicles ?? new List<Vehicle>());
            Vehicles = new ObservableCollection<Vehicle>(allVehicles);

            VehiclesByCustomer.Clear();

            OnPropertyChanged(nameof(AvailableServiceItems));
        }

        private async Task RecordMileageOnSaveIfStaleAsync(DateTime when, int? miles)
        {
            var vehicleId = SelectedVehicle?.Id ?? 0;
            if (vehicleId <= 0 || !miles.HasValue || miles.Value <= 0) return;

            var lastAt = await _mileageHistoryService.GetLastRecordTimeAsync(vehicleId, when);
            if (!lastAt.HasValue || (when - lastAt.Value) >= MileageEditLock)
            {
                await _mileageHistoryService.RecordAsync(
                    vehicleId, miles.Value, when, "Invoice", "Auto-added on Save & Print");
            }
        }

        private static bool IsVehicleFormValid(Vehicle? v) => v is not null && (
            !string.IsNullOrWhiteSpace(v.VIN) ||
            !string.IsNullOrWhiteSpace(v.LicensePlate) ||
            (!string.IsNullOrWhiteSpace(v.Make) && !string.IsNullOrWhiteSpace(v.Model))
        );

        // ===== History loading (via buttons) =====

        private bool CanLoadServiceHistoryByCustomer() =>
            (SelectedHistoryCustomer?.Id ?? 0) > 0 && !IsBusy;

        private bool CanLoadServiceHistoryByVehicle() =>
            (SelectedHistoryVehicle?.Id ?? 0) > 0 && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanLoadServiceHistoryByCustomer))]
        private async Task LoadServiceHistoryByCustomerAsync()
        {
            try
            {
                IsBusy = true;
                ServiceHistory.Clear();

                var cId = SelectedHistoryCustomer?.Id ?? 0;
                if (cId <= 0) return;

                var invoices = new List<Invoice>();
                var vehs = await _vehicleService.GetByCustomerIdAsync(cId) ?? new List<Vehicle>();
                foreach (var v in vehs)
                {
                    var vInv = await _invoiceService.GetByVehicleAsync(v.Id, includeDetails: true);
                    if (vInv != null) invoices.AddRange(vInv);
                }

                await PopulateServiceHistoryAsync(invoices);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand(CanExecute = nameof(CanLoadServiceHistoryByVehicle))]
        private async Task LoadServiceHistoryByVehicleAsync()
        {
            try
            {
                IsBusy = true;
                ServiceHistory.Clear();

                var vId = SelectedHistoryVehicle?.Id ?? 0;
                if (vId <= 0) return;

                var invoices = await _invoiceService.GetByVehicleAsync(vId, includeDetails: true)
                               ?? new List<Invoice>();

                await PopulateServiceHistoryAsync(invoices);
            }
            finally { IsBusy = false; }
        }

        // === NEW: All customers & vehicles by date range ===
        private bool CanLoadServiceHistoryByDateRange()
            => HistoryFromDate.HasValue && HistoryToDate.HasValue && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanLoadServiceHistoryByDateRange))]
        private async Task LoadServiceHistoryByDateRangeAsync()
        {
            try
            {
                if (!HistoryFromDate.HasValue || !HistoryToDate.HasValue) return;

                var from = HistoryFromDate.Value.Date;
                var to = HistoryToDate.Value.Date;

                if (to < from)
                {
                    MessageBox.Show("The 'To' date must be on or after the 'From' date.");
                    return;
                }

                IsBusy = true;
                ServiceHistory.Clear();

                // Ensure we have a list of all vehicles
                IReadOnlyList<Vehicle> allVehicles;
                if (Vehicles != null && Vehicles.Count > 0)
                {
                    allVehicles = Vehicles;
                }
                else
                {
                    var customers = await _customerService.GetAllAsync();
                    allVehicles = customers.SelectMany(c => c.Vehicles ?? new List<Vehicle>()).ToList();
                }

                // Load all invoices across vehicles
                var invoices = new List<Invoice>();
                foreach (var v in allVehicles)
                {
                    var invs = await _invoiceService.GetByVehicleAsync(v.Id, includeDetails: true);
                    if (invs != null) invoices.AddRange(invs);
                }

                // Filter by date range here to cut work for PopulateServiceHistoryAsync
                invoices = invoices
                    .Where(i => i.ServiceDate.Date >= from && i.ServiceDate.Date <= to)
                    .ToList();

                await PopulateServiceHistoryAsync(invoices);
            }
            finally { IsBusy = false; }
        }

        private async Task PopulateServiceHistoryAsync(List<Invoice> invoices)
        {
            var from = HistoryFromDate?.Date;
            var to = HistoryToDate?.Date;

            if (from.HasValue)
                invoices = invoices.Where(i => i.ServiceDate.Date >= from.Value).ToList();

            if (to.HasValue)
                invoices = invoices.Where(i => i.ServiceDate.Date <= to.Value).ToList();

            // quick lookup caches to avoid repeated searches
            var customerById = Customers?.ToDictionary(c => c.Id, c => c) ?? new Dictionary<int, Customer>();
            var vehicleById = new Dictionary<int, Vehicle>();

            void AddVehicles(IEnumerable<Vehicle>? src)
            {
                if (src == null) return;
                foreach (var v in src)
                {
                    if (v != null && v.Id > 0 && !vehicleById.ContainsKey(v.Id))
                        vehicleById[v.Id] = v;
                }
            }

            AddVehicles(Vehicles);
            AddVehicles(VehiclesByCustomer);
            AddVehicles(HistoryVehiclesByCustomer);

            foreach (var inv in invoices.OrderByDescending(i => i.ServiceDate))
            {
                // services summary
                var names = (inv.LineItems ?? new List<InvoiceLineItem>())
                    .Select(li => li.ServiceItem?.Name ?? li.Description ?? string.Empty)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                var summary = string.Join(", ", names);
                if (summary.Length > 120) summary = summary.Substring(0, 117) + "...";

                // mileage (prefer invoice value, otherwise last known on/before)
                int? rowMileage = inv.Mileage;
                if (!rowMileage.HasValue || rowMileage.Value <= 0)
                    rowMileage = await _mileageHistoryService.GetLatestOnOrBeforeAsync(inv.VehicleId, inv.ServiceDate);

                // total fallback if not persisted
                decimal total = inv.TotalAmount;
                if (total <= 0 && inv.LineItems?.Count > 0)
                    total = inv.LineItems.Sum(li => li.Price * (li.Quantity <= 0 ? 1 : li.Quantity));

                // resolve customer name
                string customerName =
                    inv.Customer?.Name
                    ?? (inv.CustomerId > 0 && customerById.TryGetValue(inv.CustomerId, out var c) ? c.Name : null)
                    ?? SelectedHistoryCustomer?.Name
                    ?? string.Empty;

                // resolve vehicle object
                Vehicle? v = inv.Vehicle;
                if (v == null && inv.VehicleId > 0 && vehicleById.TryGetValue(inv.VehicleId, out var vFound))
                    v = vFound;

                // format vehicle display: "Year Make Model [PLATE]"
                string vehicleDisplay = string.Empty;
                if (v != null)
                {
                    var parts = new List<string>();
                    if (v.Year > 0) parts.Add(v.Year.ToString());
                    if (!string.IsNullOrWhiteSpace(v.Make)) parts.Add(v.Make);
                    if (!string.IsNullOrWhiteSpace(v.Model)) parts.Add(v.Model);
                    var left = string.Join(" ", parts);
                    var plate = string.IsNullOrWhiteSpace(v.LicensePlate) ? "" : $" [{v.LicensePlate}]";
                    vehicleDisplay = (left + plate).Trim();
                }

                ServiceHistory.Add(new ServiceHistoryRow
                {
                    InvoiceId = inv.Id,
                    ServiceDate = inv.ServiceDate.Date,
                    Mileage = rowMileage,
                    Total = total,
                    ItemsSummary = summary,
                    CustomerName = customerName,
                    VehicleDisplay = vehicleDisplay
                });
            }
        }

        // When the selected history customer changes, refresh the filtered vehicles list
        partial void OnSelectedHistoryCustomerChanged(Customer? value)
        {
            SelectedHistoryVehicle = null;       // reset vehicle filter
            ServiceHistory.Clear();

            // keep the same collection instance; just refill it
            HistoryVehiclesByCustomer.Clear();
            if (value?.Id > 0)
                _ = LoadHistoryVehiclesForCustomerAsync(value.Id);

            // show history immediately for the selected customer
            _ = LoadServiceHistoryByCustomerAsync();
        }

        partial void OnSelectedHistoryVehicleChanged(Vehicle? value)
        {
            ServiceHistory.Clear();

            if (value?.Id > 0)
            {
                // specific vehicle selected ⇒ show vehicle history
                _ = LoadServiceHistoryByVehicleAsync();
            }
            else if ((SelectedHistoryCustomer?.Id ?? 0) > 0)
            {
                // vehicle cleared ⇒ fall back to customer-wide history
                _ = LoadServiceHistoryByCustomerAsync();
            }
        }

        // Enable/disable the "All in date range" button when dates change
        partial void OnHistoryFromDateChanged(DateTime? value)
            => LoadServiceHistoryByDateRangeCommand?.NotifyCanExecuteChanged();

        partial void OnHistoryToDateChanged(DateTime? value)
            => LoadServiceHistoryByDateRangeCommand?.NotifyCanExecuteChanged();

        private async Task LoadHistoryVehiclesForCustomerAsync(int customerId)
        {
            try
            {
                IsBusy = true;
                var list = await _vehicleService.GetByCustomerIdAsync(customerId) ?? new List<Vehicle>();

                HistoryVehiclesByCustomer.Clear();
                foreach (var v in list)
                    HistoryVehiclesByCustomer.Add(v);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand] private void Refresh() => _ = LoadDataAsync();

        [RelayCommand]
        private void RemoveServiceItem(ServiceItemRow row)
        {
            if (row is null) return;
            if (SelectedServices.Contains(row))
                SelectedServices.Remove(row);
        }

        [RelayCommand]
        private async Task PrintInvoiceAsync()
        {
            // PDF only — no DB writes.
            var moment = ServiceMomentNow;

            // 1) Customer for PDF (prefer edited form)
            Customer? customerForPdf = !string.IsNullOrWhiteSpace(NewCustomer?.Name)
                ? NewCustomer
                : SelectedCustomer;

            if (customerForPdf == null || string.IsNullOrWhiteSpace(customerForPdf.Name))
            {
                MessageBox.Show("Please enter customer details (at least Name) to print.");
                return;
            }

            // 2) Vehicle for PDF (must have real info)
            bool formVehicleHasInfo =
                Vehicle != null && (
                    !string.IsNullOrWhiteSpace(Vehicle.VIN) ||
                    !string.IsNullOrWhiteSpace(Vehicle.LicensePlate) ||
                    (!string.IsNullOrWhiteSpace(Vehicle.Make) && !string.IsNullOrWhiteSpace(Vehicle.Model))
                );

            Vehicle? vehicleForPdf = formVehicleHasInfo ? Vehicle : SelectedVehicle;

            if (vehicleForPdf == null)
            {
                MessageBox.Show("Please enter vehicle details (VIN or License Plate, or Make & Model) or select a saved vehicle before printing.");
                return;
            }

            // 3) Build line items from grid rows
            var validItems = SelectedServices
                .Where(r => r.Price > 0 && r.Quantity > 0)
                .Select(r => new InvoiceLineItem
                {
                    ServiceItemId = r.Id, // null for "Other"
                    Description = string.IsNullOrWhiteSpace(r.Name) ? "Other" : r.Name,
                    Price = r.Price,
                    Quantity = r.Quantity
                })
                .ToList();

            if (!validItems.Any())
            {
                MessageBox.Show("Please add at least one service with a price greater than 0.");
                return;
            }

            // 4) In-memory invoice for PDF only
            var invoice = new Invoice
            {
                CustomerId = SelectedCustomer?.Id ?? 0,
                VehicleId = SelectedVehicle?.Id ?? 0,
                Customer = customerForPdf,
                Vehicle = vehicleForPdf,
                ServiceDate = moment,
                Mileage = GetMileage(),
                LineItems = validItems
            };
            foreach (var li in invoice.LineItems) li.Invoice = invoice;

            try
            {
                await _invoicePdfService.GenerateAsync(invoice, CancellationToken.None);
            }
            catch (Exception ex)
            {
                LogErrorToFile(ex);
                MessageBox.Show("Failed to generate PDF. Check logs for details.");
            }
        }

        // Gate "New Invoice" by NewInvoiceEnabled flag
        private bool CanCreateNewInvoice() => NewInvoiceEnabled && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanCreateNewInvoice))]
        private void CreateNewInvoice()
        {
            // Clear selections & edit buffers
            SelectedCustomer = null;
            NewCustomer = new Customer();

            SelectedVehicle = null;
            Vehicle = new Vehicle();
            VehiclesByCustomer.Clear();

            // Clear invoice items & totals
            SelectedServices.Clear();
            RecalculateTotal();
            ServiceDate = DateTime.Now;

            // Reset mileage
            Mileage = null;
            MileageText = string.Empty;

            // Reset duplicate-safe lock for Save & Print
            HasSubmittedCurrentInvoice = false;

            // Disable "New Invoice" again until the next successful save
            NewInvoiceEnabled = false;

            // Optional UI marker
            NewInvoiceMode = true;
        }

        // Save & Print is enabled by default; only disabled if busy or already submitted
        private bool CanSaveAndPrintInvoice() => !IsBusy && !HasSubmittedCurrentInvoice;

        [RelayCommand(CanExecute = nameof(CanSaveAndPrintInvoice))]
        private async Task SaveAndPrintInvoiceAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Vehicle ??= new Vehicle(); // ensure bindings have a target

                var moment = ServiceMomentNow;
                var miles = GetMileage();

                // 0) Build + validate line items from the grid
                var rows = SelectedServices ?? Enumerable.Empty<ServiceItemRow>();
                var rawItems = rows
                    .Where(r => r is not null && r.Price > 0 && r.Quantity > 0)
                    .Select(r => new InvoiceLineItem
                    {
                        ServiceItemId = (r.Id > 0) ? r.Id : (int?)null,  // null => "Other"
                        Description = string.IsNullOrWhiteSpace(r.Name) ? "Other" : r.Name.Trim(),
                        Price = Math.Round(r.Price, 2),
                        Quantity = r.Quantity
                    })
                    .ToList();

                if (rawItems.Count == 0)
                {
                    MessageBox.Show("Please add at least one service with price > 0.");
                    return;
                }

                // De-duplicate: by ServiceItemId or Description
                var items = rawItems
                    .GroupBy(i => i.ServiceItemId.HasValue
                        ? $"ID:{i.ServiceItemId.Value}"
                        : $"DESC:{(i.Description ?? string.Empty).Trim().ToLowerInvariant()}|P:{Math.Round(i.Price, 2):0.00}")
                    .Select(g => g.First())
                    .ToList();

                await MapCustomItemsToCatalogAsync(items);

                // --- confirm duplicates within last 5 days for the SAME vehicle ---
                var hasExistingVehicleForConfirm = (SelectedVehicle?.Id ?? 0) > 0;
                if (hasExistingVehicleForConfirm)
                {
                    var ok = await ConfirmDuplicateServicesAsync(SelectedVehicle!.Id, items, moment);
                    if (!ok) return; // user chose "No" -> abort save
                }
                // -----------------------------------------------------------------------

                // 1) Decide: first-time save (create C/V) vs invoice-only
                var isNewCustomer = SelectedCustomer is null || SelectedCustomer.Id <= 0;
                var isNewVehicle = SelectedVehicle is null || SelectedVehicle.Id <= 0;

                Customer? savedCustomer = SelectedCustomer;
                Vehicle? savedVehicle = SelectedVehicle;

                if (isNewCustomer)
                {
                    if (string.IsNullOrWhiteSpace(NewCustomer?.Name))
                    {
                        MessageBox.Show("Please enter customer details (at least Name).");
                        return;
                    }

                    savedCustomer = await _customerService.AddOrUpdateAsync(new Customer
                    {
                        Id = 0,
                        Name = NewCustomer.Name?.Trim(),
                        Phone = NewCustomer.Phone?.Trim(),
                        Email = NewCustomer.Email?.Trim(),
                        AddressLine1 = NewCustomer.AddressLine1?.Trim()
                    });

                    // Don't let SelectedCustomer change wipe the items
                    _suppressCustomerChangeSideEffects = true;
                    try { SelectedCustomer = savedCustomer; }
                    finally { _suppressCustomerChangeSideEffects = false; }
                }
                else
                {
                    savedCustomer = SelectedCustomer;
                }

                if (isNewVehicle)
                {




                    if (!HasVehicleInput(Vehicle))



                    {
                        MessageBox.Show("Please enter vehicle details (VIN or License Plate, or Make & Model).");
                        return;
                    }

                    try
                    {
                        savedVehicle = await _vehicleService.AddOrUpdateAsync(new Vehicle
                        {
                            Id = 0,
                            Make = Vehicle!.Make?.Trim(),
                            Model = Vehicle!.Model?.Trim(),
                            Year = Vehicle!.Year,
                            LicensePlate = Vehicle!.LicensePlate?.Trim(),
                            VIN = Vehicle!.VIN?.Trim(),
                            Color = Vehicle!.Color?.Trim(),
                            Engine = Vehicle!.Engine?.Trim(),
                            Transmission = Vehicle!.Transmission?.Trim(),
                            FuelType = Vehicle!.FuelType?.Trim(),
                            CustomerId = (savedCustomer ?? SelectedCustomer)!.Id
                        });

                        SelectedVehicle = savedVehicle;
                    }
                    catch (InvalidOperationException ex) when (
                        ex.Message.IndexOf("same License Plate or VIN", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        MessageBox.Show(ex.Message, "Duplicate Vehicle", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return; // abort
                    }



                }

                if ((savedCustomer ?? SelectedCustomer) is null || (savedVehicle ?? SelectedVehicle) is null)
                {
                    MessageBox.Show("Select an existing customer/vehicle or enter details to create them the first time.");
                    return;
                }

                // 2) Mileage — add only if no record in the last 5 hours
                await RecordMileageOnSaveIfStaleAsync(moment, miles);

                // 3) Save invoice (FK IDs only)
                var invoiceToSave = new Invoice
                {
                    CustomerId = (savedCustomer ?? SelectedCustomer)!.Id,
                    VehicleId = (savedVehicle ?? SelectedVehicle)!.Id,
                    ServiceDate = moment,
                    Mileage = miles,
                    LineItems = items
                };

                foreach (var li in invoiceToSave.LineItems)
                    li.ServiceItem = null;

                int invoiceId = await _invoiceService.AddAsync(invoiceToSave);

                // 4) Generate PDF (detached object)
                try
                {
                    var pdfInvoice = new Invoice
                    {
                        Id = invoiceId,
                        CustomerId = (savedCustomer ?? SelectedCustomer)!.Id,
                        VehicleId = (savedVehicle ?? SelectedVehicle)!.Id,
                        Customer = (savedCustomer ?? SelectedCustomer),
                        Vehicle = (savedVehicle ?? SelectedVehicle),
                        ServiceDate = moment,
                        Mileage = miles,
                        LineItems = items
                    };
                    await _invoicePdfService.GenerateAsync(pdfInvoice, CancellationToken.None);
                }
                catch (Exception pdfEx)
                {
                    LogErrorToFile(pdfEx);
                }

                // Successful submit -> lock Save & Print and enable New Invoice
                HasSubmittedCurrentInvoice = true; // prevents duplicate submit
                NewInvoiceEnabled = true;          // allows starting a fresh invoice

                // re-check Add New Vehicle / Customer button enablement after submit
                AddNewVehicleCommand?.NotifyCanExecuteChanged();
                AddNewCustomerCommand?.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                LogErrorToFile(ex);
                MessageBox.Show($"Failed to save and print invoice.\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void UnlockAdmin()
        {
            var owner = System.Windows.Application.Current?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
            var dlg = new AdminPasswordDialog(_authService) { Owner = owner };
            if (dlg.ShowDialog() == true && dlg.IsAuthenticated)
                IsAdminUnlocked = true;
        }

        [RelayCommand] private void LockAdmin() => IsAdminUnlocked = false;

        [RelayCommand]
        private async Task ChangeAdminPassword()
        {
            var owner = System.Windows.Application.Current?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
            var dlg = new ChangePasswordDialog() { Owner = owner };
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.NewPassword))
                await _authService.SetAdminPasswordAsync(dlg.NewPassword);
        }

        [RelayCommand]
        private async Task ResetFormAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                SelectedCustomer = null;
                NewCustomer = new Customer();

                SelectedVehicle = null;
                Vehicle = new Vehicle();

                SelectedServiceItem = null;
                SelectedServices.Clear();

                Mileage = null; MileageText = string.Empty;
                ServiceDate = DateTime.Today;

                Total = 0m;

                OnPropertyChanged(nameof(AvailableServiceItems));

                ServiceHistory.Clear();

                // reset modes
                NewInvoiceMode = false;
                HasSubmittedCurrentInvoice = false; // allow Save & Print after reset
                NewInvoiceEnabled = false;          // keep New Invoice disabled after reset

                // Reload from DB
                await RefreshLookupsAsync();

                OnPropertyChanged(nameof(AvailableServiceItems));
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void CloseApp(System.Windows.Window? window)
        {
            window?.Close(); // closes MainWindow -> app exits (default ShutdownMode)
        }

        partial void OnNewCustomerModeChanged(bool value)
            => SaveNewCustomerCommand.NotifyCanExecuteChanged();

        partial void OnIsBusyChanged(bool value)
        {
            SaveNewCustomerCommand?.NotifyCanExecuteChanged();
            SaveNewVehicleCommand?.NotifyCanExecuteChanged();
            SaveAndPrintInvoiceCommand?.NotifyCanExecuteChanged();
            AddNewVehicleCommand?.NotifyCanExecuteChanged();
            CreateNewInvoiceCommand?.NotifyCanExecuteChanged();
            AddNewCustomerCommand?.NotifyCanExecuteChanged();
            // also refresh history buttons
            LoadServiceHistoryByCustomerCommand?.NotifyCanExecuteChanged();
            LoadServiceHistoryByVehicleCommand?.NotifyCanExecuteChanged();
            LoadServiceHistoryByDateRangeCommand?.NotifyCanExecuteChanged(); // NEW
        }

        // *** Add New Customer is disabled until a customer is selected, AND stays disabled after Save&Print
        private bool CanStartAddCustomer() =>
            (SelectedCustomer?.Id ?? 0) > 0 && !IsBusy && !HasSubmittedCurrentInvoice;

        [RelayCommand(CanExecute = nameof(CanStartAddCustomer))]
        private void AddNewCustomer()
        {
            SelectedCustomer = null;
            VehiclesByCustomer.Clear();
            NewCustomer = new Customer();
            SelectedVehicle = null;
            Vehicle = null;
            Mileage = null;
            MileageText = string.Empty;
            ServiceHistory.Clear();

            NewCustomerMode = true;   // enables Save New Customer
            NewVehicleMode = false;

            SaveNewVehicleCommand.NotifyCanExecuteChanged();
            SaveNewCustomerCommand.NotifyCanExecuteChanged();
        }

        private bool CanSaveNewCustomer() => NewCustomerMode && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanSaveNewCustomer))]
        private async Task SaveNewCustomerAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewCustomer?.Name))
                {
                    MessageBox.Show("Please enter customer details (at least Name).");
                    return;
                }

                // sanitize inputs
                var toSave = new Customer
                {
                    Id = 0, // force insert
                    Name = NewCustomer.Name.Trim(),
                    Phone = NewCustomer.Phone?.Trim(),
                    Email = NewCustomer.Email?.Trim(),
                    AddressLine1 = NewCustomer.AddressLine1?.Trim()
                };

                var saved = await _customerService.AddOrUpdateAsync(toSave);

                // Refresh lookups and select saved customer
                await RefreshLookupsAsync();
                SelectedCustomer = Customers.FirstOrDefault(c => c.Id == saved.Id) ?? saved;

                // Reflect saved values back into the edit buffer
                NewCustomer = new Customer
                {
                    Id = saved.Id,
                    Name = saved.Name,
                    Phone = saved.Phone,
                    Email = saved.Email,
                    AddressLine1 = saved.AddressLine1
                };

                // Load (empty) vehicles list for the new customer
                _ = LoadVehiclesForCustomerAsync(saved.Id);

                NewCustomerMode = false; // disable "Save New Customer"
            }
            catch (Exception ex)
            {
                LogErrorToFile(ex);
                MessageBox.Show($"Failed to save customer.\n{ex.Message}");
            }
        }

        partial void OnNewVehicleModeChanged(bool value) => SaveNewVehicleCommand?.NotifyCanExecuteChanged();

        // *** Add New Vehicle enabled ONLY when a vehicle is selected (Id > 0) ***
        // and NOT blocked by HasSubmittedCurrentInvoice (so it's enabled after Save&Print if a vehicle is selected)
        private bool CanStartAddVehicle()
            => (SelectedVehicle?.Id ?? 0) > 0 && !IsAddingVehicle && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanStartAddVehicle))]
        private void AddNewVehicle()
        {
            if ((SelectedVehicle?.Id ?? 0) <= 0)
            {
                MessageBox.Show("Select an existing vehicle first.");
                return;
            }

            // Use the same customer as the currently selected vehicle
            var customerId = SelectedVehicle!.CustomerId;
            if (customerId <= 0)
            {
                MessageBox.Show("Cannot determine customer for the new vehicle.");
                return;
            }

            // Ensure SelectedCustomer matches the vehicle's customer (if available)
            if (SelectedCustomer?.Id != customerId)
                SelectedCustomer = Customers.FirstOrDefault(c => c.Id == customerId);

            // 1) Clear selection so it doesn't repopulate the form
            SelectedVehicle = null;

            // 2) Start a blank vehicle tied to the current customer
            Vehicle = new Vehicle { CustomerId = customerId };

            // 3) Reset mileage
            Mileage = null;
            MileageText = string.Empty;

            // 4) Clear per-vehicle service history view
            ServiceHistory?.Clear();

            // 5) Enable Save New Vehicle right away (button gated by NewVehicleMode)
            NewVehicleMode = true;
        }

        // Enable Save New Vehicle as soon as NewVehicleMode is on (no form validity gate here)
        private bool CanSaveNewVehicle() =>
            NewVehicleMode &&
            (SelectedCustomer?.Id ?? 0) > 0 &&
            !IsBusy;

        [RelayCommand(CanExecute = nameof(CanSaveNewVehicle))]
        private async Task SaveNewVehicleAsync()
        {
            try
            {
                // Strong guard: block invalid saves
                if (!IsVehicleFormValid(Vehicle))
                {
                    MessageBox.Show("Enter at least VIN or License Plate, or both Make & Model before saving.");
                    return;
                }

                // sanitize inputs
                var v = Vehicle!;
                v.Make = v.Make?.Trim();
                v.Model = v.Model?.Trim();
                v.LicensePlate = v.LicensePlate?.Trim();
                v.VIN = v.VIN?.Trim();
                v.Color = v.Color?.Trim();
                v.Engine = v.Engine?.Trim();
                v.Transmission = v.Transmission?.Trim();
                v.FuelType = v.FuelType?.Trim();
                v.CustomerId = SelectedCustomer!.Id;

                try
                {
                    var saved = await _vehicleService.AddOrUpdateAsync(new Vehicle
                    {
                        Id = 0,
                        Make = v.Make,
                        Model = v.Model,
                        Year = v.Year,
                        LicensePlate = v.LicensePlate,
                        VIN = v.VIN,
                        Color = v.Color,
                        Engine = v.Engine,
                        Transmission = v.Transmission,
                        FuelType = v.FuelType,
                        CustomerId = v.CustomerId
                    });

                    // refresh and select
                    await LoadVehiclesForCustomerAsync(SelectedCustomer!.Id);
                    SelectedVehicle = VehiclesByCustomer.FirstOrDefault(x => x.Id == saved.Id) ?? saved;
                    Vehicle = SelectedVehicle;

                    // mileage (simple 5h rule)
                    var moment = ServiceMomentNow;
                    var miles = GetMileage();
                    await RecordMileageOnSaveIfStaleAsync(moment, miles);

                    NewVehicleMode = false;
                }
                catch (InvalidOperationException ex) when (
                    ex.Message.IndexOf("same License Plate or VIN", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    MessageBox.Show(ex.Message, "Duplicate Vehicle", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // abort
                }
            }
            catch (Exception ex)
            {
                LogErrorToFile(ex);
                MessageBox.Show($"Failed to save vehicle.\n{ex.Message}");
            }
        }

        private void LogErrorToFile(Exception ex)
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // ignore logging errors
            }
        }
    }
}

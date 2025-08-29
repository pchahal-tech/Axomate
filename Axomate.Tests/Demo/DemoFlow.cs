using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Axomate.Tests.Contracts;
using Axomate.Tests.TestConfig;

namespace Axomate.Tests.Demo
{
#if USE_DEMO_FLOW
    public sealed class DemoFlow : IMainFlow
    {
        private readonly ICustomerService _customerService;
        private readonly IVehicleService _vehicleService;
        private readonly IInvoiceService _invoiceService;
        private readonly IMileageHistoryRepository _mileageRepo;
        private readonly IPdfService _pdf;
        private readonly ILogger _logger;
        private readonly ITimeProvider _time;

        public DemoFlow(
            ICustomerService cs, IVehicleService vs, IInvoiceService inv,
            IMileageHistoryRepository mileage, IPdfService pdf, ILogger logger, ITimeProvider time)
        {
            _customerService = cs;
            _vehicleService = vs;
            _invoiceService = inv;
            _mileageRepo = mileage;
            _pdf = pdf;
            _logger = logger;
            _time = time;
        }

        // Modes
        public bool NewCustomerMode { get; private set; }
        public bool NewVehicleMode { get; private set; }
        public bool NewInvoiceMode { get; private set; }
        public bool IsBusy { get; private set; }

        // Selection & buffers
        public Customer? SelectedCustomer { get; private set; }
        public Vehicle? SelectedVehicle { get; private set; }
        public Customer NewCustomer { get; set; } = new();
        public Vehicle Vehicle { get; set; } = new();

        // Mileage (re-entrancy-safe)
        private bool _suppressMileageHandler;
        private string? _mileageText;

        public string? MileageText
        {
            get => _mileageText;
            set
            {
                if (string.Equals(_mileageText, value, StringComparison.Ordinal)) return;
                _mileageText = value;
                if (!_suppressMileageHandler)
                {
                    _ = HandleMileageTextChangedAsync(value);
                }
            }
        }

        public int? Mileage { get; private set; }
        public DateTime ServiceDate { get; set; } = DateTime.Today;

        // Line Items
        public IList<LineItem> LineItems { get; } = new List<LineItem>();
        public void AddLineItem(LineItem item) => LineItems.Add(item);
        public void ClearLineItems() => LineItems.Clear();

        public string? LastUserMessage { get; private set; }

        public bool CanSaveNewCustomer => NewCustomerMode;
        public bool CanSaveNewVehicle => NewVehicleMode && (SelectedCustomer?.Id > 0) && ValidateVehicle();
        public bool CanSaveAndPrint => NewInvoiceMode && !IsBusy;

        public void AddNewCustomer()
        {
            SelectedCustomer = null;
            SelectedVehicle = null;
            NewCustomer = new();
            Vehicle = new();
            Mileage = null;
            MileageText = null;
            NewCustomerMode = true;
            NewVehicleMode = false;
            LastUserMessage = null;
        }

        public async Task SaveNewCustomerAsync()
        {
            if (!NewCustomerMode) return;
            try
            {
                if (string.IsNullOrWhiteSpace(NewCustomer.Name))
                    throw new ArgumentException("Name is required.");
                var id = await _customerService.AddOrUpdateAsync(NewCustomer with { Name = NewCustomer.Name.Trim() });
                SelectedCustomer = NewCustomer with { Id = id };
                NewCustomerMode = false;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save customer.", ex);
                LastUserMessage = ex is ArgumentException ? ex.Message : "Could not save customer.";
            }
        }

        public void AddNewVehicle()
        {
            if (SelectedCustomer is null || SelectedCustomer.Id <= 0)
            {
                LastUserMessage = "Select or save a customer first.";
                return;
            }
            SelectedVehicle = null;
            Vehicle = new Vehicle { CustomerId = SelectedCustomer.Id };
            Mileage = null;
            MileageText = null;
            NewVehicleMode = true;
            LastUserMessage = null;
        }

        private bool ValidateVehicle()
        {
            return !string.IsNullOrWhiteSpace(Vehicle.VIN) ||
                   !string.IsNullOrWhiteSpace(Vehicle.LicensePlate) ||
                   (!string.IsNullOrWhiteSpace(Vehicle.Make) && !string.IsNullOrWhiteSpace(Vehicle.Model));
        }

        public async Task SaveNewVehicleAsync()
        {
            if (!NewVehicleMode) return;
            if (SelectedCustomer is null || SelectedCustomer.Id <= 0)
            {
                LastUserMessage = "Select or save a customer first.";
                return;
            }
            try
            {
                var v = Vehicle with { CustomerId = SelectedCustomer.Id };
                var id = await _vehicleService.AddOrUpdateAsync(v);
                SelectedVehicle = v with { Id = id };
                await RecordMileageIfApplicableAsync(onSave: true);
                NewVehicleMode = false;
            }
            catch (DuplicateResourceException dup)
            {
                _logger.Error("Duplicate vehicle.", dup);
                LastUserMessage = "A vehicle with this license plate already exists.";
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save vehicle.", ex);
                LastUserMessage = ex is ArgumentException ? ex.Message : "Could not save vehicle.";
            }
        }

        public void CreateNewInvoice()
        {
            LineItems.Clear();
            ServiceDate = _time.NowLocal;
            Mileage = null;
            MileageText = null;
            NewInvoiceMode = true;
            LastUserMessage = null;
        }

        private int _busyToken; // re-entrancy guard for SaveAndPrint


        public Task SaveAndPrintInvoiceAsync()
        {
            // Re-entrancy guard: if already busy, ignore
            if (System.Threading.Interlocked.Exchange(ref _busyToken, 1) == 1)
                return Task.CompletedTask;

            // ✅ Set immediately so the test sees IsBusy == true right after the call
            IsBusy = true;

            // Hand off to async core
            return SaveAndPrintCoreAsync();
        }

        private async Task SaveAndPrintCoreAsync()
        {
            try
            {
                // If not in invoice mode, just exit (finally will clear IsBusy)
                if (!NewInvoiceMode) return;

                // ✅ Give control back to the caller now, with IsBusy already true
                await Task.Yield();

                if (SelectedCustomer is null || SelectedVehicle is null)
                    throw new InvalidOperationException("Customer and vehicle must be selected or created.");

                if (LineItems.Count == 0)
                    throw new ArgumentException("At least one line item required.");

                if (LineItems.Any(li => li.Price <= 0m || li.Quantity < 1))
                    throw new ArgumentException("Invalid line item.");

                await RecordMileageIfApplicableAsync(onSave: true);

                var invoice = new Invoice
                {
                    CustomerId = SelectedCustomer.Id,
                    VehicleId = SelectedVehicle.Id,
                    ServiceDate = ComposeServiceMoment(),
                    Mileage = Mileage,
                    LineItems = LineItems.ToList()
                };

                var id = await _invoiceService.AddAsync(invoice);
                await _pdf.GenerateAsync(invoice with { Id = id }, ComposeServiceMoment().ToUniversalTime());

                NewInvoiceMode = false;
            }
            catch (DuplicateResourceException dup)
            {
                _logger.Error("Duplicate invoice.", dup);
                LastUserMessage = "An invoice for this customer/vehicle/date already exists.";
            }
            catch (Exception ex)
            {
                _logger.Error("Save & Print failed.", ex);
                LastUserMessage = ex is ArgumentException ? ex.Message : "Save & Print failed.";
            }
            finally
            {
                IsBusy = false;
                System.Threading.Interlocked.Exchange(ref _busyToken, 0);
            }
        }

        public void ResetForm()
        {
            NewCustomerMode = false;
            NewVehicleMode = false;
            NewInvoiceMode = false;
            IsBusy = false;
            SelectedCustomer = null;
            SelectedVehicle = null;
            NewCustomer = new();
            Vehicle = new();
            LineItems.Clear();
            Mileage = null;
            MileageText = null;
            LastUserMessage = null;
        }

        private async Task HandleMileageTextChangedAsync(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Mileage = null;
                return;
            }
            if (int.TryParse(text.Trim(), out var m) && m > 0)
            {
                await TryRecordMileageChangeAsync(m);
            }
        }

        private async Task TryRecordMileageChangeAsync(int newMileage)
        {
            if (SelectedVehicle is null) { Mileage = newMileage; return; }

            var nowUtc = _time.UtcNow;
            var lastAt = await _mileageRepo.GetLatestRecordedAtOnOrBeforeAsync(SelectedVehicle.Id, _time.NowLocal);
            var lastVal = await _mileageRepo.GetLatestMileageOnOrBeforeAsync(SelectedVehicle.Id, _time.NowLocal);

            if (lastVal is not null && newMileage < lastVal)
            {
                Mileage = lastVal;

                _suppressMileageHandler = true;
                try { MileageText = lastVal.ToString(); }
                finally { _suppressMileageHandler = false; }

                LastUserMessage = "Mileage cannot decrease. Reverted to last recorded value.";
                return;
            }

            if (lastAt is not null && (nowUtc - lastAt.Value.ToUniversalTime()) < TimeSpan.FromHours(5))
            {
                Mileage = lastVal ?? newMileage;

                _suppressMileageHandler = true;
                try { MileageText = Mileage?.ToString(); }
                finally { _suppressMileageHandler = false; }

                var remaining = TimeSpan.FromHours(5) - (nowUtc - lastAt.Value.ToUniversalTime());
                LastUserMessage = $"Mileage edit locked. Try again in {remaining.Hours:D2}:{remaining.Minutes:D2}h.";
                return;
            }

            Mileage = newMileage;
            await _mileageRepo.AddAsync(SelectedVehicle.Id, newMileage, nowUtc, "typing", null);
        }

        private async Task RecordMileageIfApplicableAsync(bool onSave)
        {
            if (SelectedVehicle is null || Mileage is null) return;

            var nowUtc = _time.UtcNow;
            var lastAt = await _mileageRepo.GetLatestRecordedAtOnOrBeforeAsync(SelectedVehicle.Id, _time.NowLocal);
            if (lastAt is not null && (nowUtc - lastAt.Value.ToUniversalTime()) < TimeSpan.FromHours(5))
            {
                return;
            }
            await _mileageRepo.AddAsync(SelectedVehicle.Id, Mileage.Value, nowUtc, onSave ? "save" : "prefill", null);
        }

        private DateTime ComposeServiceMoment()
        {
            var now = _time.NowLocal;
            var date = ServiceDate.Date;
            return date.Add(now.TimeOfDay);
        }
    }
#endif
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Axomate.Tests.Contracts;
using Axomate.Tests.TestConfig;

namespace Axomate.Tests.Demo
{
    public interface ICustomerService
    {
        Task<int> AddOrUpdateAsync(Customer c);
    }

    public interface IVehicleService
    {
        Task<int> AddOrUpdateAsync(Vehicle v);
    }

    public interface IInvoiceService
    {
        Task<int> AddAsync(Invoice invoice);
    }

    public interface IMileageHistoryRepository
    {
        Task<int?> GetLatestMileageOnOrBeforeAsync(int vehicleId, DateTime referenceDate);
        Task<DateTime?> GetLatestRecordedAtOnOrBeforeAsync(int vehicleId, DateTime referenceDate);
        Task<int> AddAsync(int vehicleId, int mileage, DateTime recordedAtUtc, string source, string? notes);
    }

    public interface IPdfService
    {
        Task GenerateAsync(Invoice invoice, DateTime generatedAtUtc);
        (Invoice? last, DateTime? atUtc) Snapshot();
    }

    public interface ILogger
    {
        void Error(string message, Exception ex);
    }

    public sealed class ListLogger : ILogger
    {
        public List<(string Message, Exception Ex)> Entries { get; } = new();
        public void Error(string message, Exception ex) => Entries.Add((message, ex));
    }

    public sealed class InMemoryServices :
        ICustomerService, IVehicleService, IInvoiceService, IMileageHistoryRepository, IPdfService
    {
        private int _customerId = 0;
        private int _vehicleId = 0;
        private int _invoiceId = 0;

        public List<Customer> Customers { get; } = new();
        public List<Vehicle> Vehicles { get; } = new();
        public List<Invoice> Invoices { get; } = new();

        // vehicleId -> list of (mileage, recordedAtUtc)
        public Dictionary<int, List<(int mileage, DateTime atUtc)>> Mileage { get; } = new();

        private (Invoice? inv, DateTime? atUtc) _pdf;

        public Task<int> AddOrUpdateAsync(Customer c)
        {
            if (string.IsNullOrWhiteSpace(c.Name)) throw new ArgumentException("Name is required.");
            var existing = Customers.FirstOrDefault(x => x.Id == c.Id);
            if (existing is null)
            {
                var id = ++_customerId;
                Customers.Add(c with { Id = id });
                return Task.FromResult(id);
            }
            else
            {
                Customers.Remove(existing);
                Customers.Add(c);
                return Task.FromResult(c.Id);
            }
        }

        public Task<int> AddOrUpdateAsync(Vehicle v)
        {
            if (!string.IsNullOrWhiteSpace(v.LicensePlate) &&
                Vehicles.Any(x => x.LicensePlate == v.LicensePlate && x.Id != v.Id))
            {
                throw new DuplicateResourceException("Duplicate license plate.");
            }

            var valid = (!string.IsNullOrWhiteSpace(v.VIN)) ||
                        (!string.IsNullOrWhiteSpace(v.LicensePlate)) ||
                        (!string.IsNullOrWhiteSpace(v.Make) && !string.IsNullOrWhiteSpace(v.Model));

            if (!valid) throw new ArgumentException("Vehicle requires VIN or LicensePlate or Make+Model.");

            var existing = Vehicles.FirstOrDefault(x => x.Id == v.Id);
            if (existing is null)
            {
                var id = ++_vehicleId;
                Vehicles.Add(v with { Id = id });
                return Task.FromResult(id);
            }
            else
            {
                Vehicles.Remove(existing);
                Vehicles.Add(v);
                return Task.FromResult(v.Id);
            }
        }

        public Task<int> AddAsync(Invoice invoice)
        {
            if (Invoices.Any(x => x.CustomerId == invoice.CustomerId &&
                                  x.VehicleId == invoice.VehicleId &&
                                  x.ServiceDate.Date == invoice.ServiceDate.Date))
            {
                throw new DuplicateResourceException("Duplicate invoice for same customer, vehicle, and date.");
            }

            if (invoice.LineItems is null || invoice.LineItems.Count == 0)
                throw new ArgumentException("At least one line item required.");

            if (invoice.LineItems.Any(li => li.Price <= 0m || li.Quantity < 1))
                throw new ArgumentException("Invalid line item.");

            var id = ++_invoiceId;
            Invoices.Add(invoice with { Id = id });
            return Task.FromResult(id);
        }

        public Task<int?> GetLatestMileageOnOrBeforeAsync(int vehicleId, DateTime referenceDate)
        {
            if (!Mileage.TryGetValue(vehicleId, out var list) || list.Count == 0) return Task.FromResult<int?>(null);
            var best = list.Where(x => x.atUtc <= referenceDate.ToUniversalTime())
                           .OrderByDescending(x => x.atUtc).ThenByDescending(x => x.mileage).FirstOrDefault();
            return Task.FromResult<int?>(best.mileage);
        }

        public Task<DateTime?> GetLatestRecordedAtOnOrBeforeAsync(int vehicleId, DateTime referenceDate)
        {
            if (!Mileage.TryGetValue(vehicleId, out var list) || list.Count == 0) return Task.FromResult<DateTime?>(null);
            var best = list.Where(x => x.atUtc <= referenceDate.ToUniversalTime())
                           .OrderByDescending(x => x.atUtc).FirstOrDefault();
            return Task.FromResult<DateTime?>(best.atUtc);
        }

        public Task<int> AddAsync(int vehicleId, int mileage, DateTime recordedAtUtc, string source, string? notes)
        {
            if (!Mileage.TryGetValue(vehicleId, out var list))
            {
                list = new();
                Mileage[vehicleId] = list;
            }
            list.Add((mileage, recordedAtUtc));
            return Task.FromResult(list.Count);
        }

        public Task GenerateAsync(Invoice invoice, DateTime generatedAtUtc)
        {
            _pdf = (invoice, generatedAtUtc);
            return Task.CompletedTask;
        }

        public (Invoice? last, DateTime? atUtc) Snapshot() => _pdf;
    }
}

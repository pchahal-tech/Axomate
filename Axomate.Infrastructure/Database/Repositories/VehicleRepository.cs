// File: Axomate.Infrastructure/Database/Repositories/VehicleRepository.cs
using System.Security.Cryptography;
using System.Text;
using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly AxomateDbContext _db;

        public VehicleRepository(AxomateDbContext db)
        {
            _db = db;
        }

        // Create
        public async Task<int> AddAsync(Vehicle vehicle)
        {
            // If a Customer object is attached, make sure EF doesn’t try to insert it
            if (vehicle.Customer != null)
            {
                vehicle.CustomerId = vehicle.Customer.Id;
                _db.Entry(vehicle.Customer).State = EntityState.Unchanged;
            }

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();
            return vehicle.Id;
        }

        // Update (returns updated entity for consistency)
        public async Task<Vehicle> UpdateAsync(Vehicle vehicle)
        {
            // Protect against accidental Customer re-insert
            if (vehicle.Customer != null)
            {
                vehicle.CustomerId = vehicle.Customer.Id;
                _db.Entry(vehicle.Customer).State = EntityState.Unchanged;
            }

            _db.Vehicles.Update(vehicle);
            await _db.SaveChangesAsync();
            return vehicle;
        }

        // Delete
        public async Task DeleteAsync(int id)
        {
            var entity = await _db.Vehicles.FindAsync(id);
            if (entity != null)
            {
                _db.Vehicles.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }

        // Read: single (optionally include Customer)
        public async Task<Vehicle?> GetByIdAsync(int id, bool includeCustomer = true)
        {
            var query = _db.Vehicles.AsQueryable();
            if (includeCustomer)
                query = query.Include(v => v.Customer);

            return await query.FirstOrDefaultAsync(v => v.Id == id);
        }

        // Read: vehicles for a customer
        public async Task<List<Vehicle>> GetByCustomerAsync(int customerId)
        {
            return await _db.Vehicles
                .AsNoTracking()
                .Where(v => v.CustomerId == customerId)
                .OrderBy(v => v.Make)
                .ThenBy(v => v.Model)
                .ThenBy(v => v.Year)
                .ToListAsync();
        }

        // Read: all vehicles
        public async Task<List<Vehicle>> GetAllAsync()
        {
            return await _db.Vehicles
                .AsNoTracking()
                .Include(v => v.Customer)
                .OrderBy(v => v.Make)
                .ThenBy(v => v.Model)
                .ThenBy(v => v.Year)
                .ToListAsync();
        }

        /// <summary>
        /// Duplicate check using shadow hash columns (LicensePlateHash / VinHash).
        /// Matches the normalization+hashing rules in AxomateDbContext.
        /// </summary>
        public async Task<bool> ExistsByPlateOrVinAsync(string licensePlate, string? vin, int? excludeVehicleId = null)
        {
            string? plateHash = HashOrNull(NormUpper(licensePlate));
            string? vinHash = HashOrNull(NormUpper(vin));

            if (plateHash is null && vinHash is null)
                return false;

            IQueryable<Vehicle> q = _db.Vehicles.AsNoTracking();

            if (excludeVehicleId is int id)
                q = q.Where(v => v.Id != id);

            var byPlateTask = plateHash is not null
                ? q.Where(v => EF.Property<string>(v, "LicensePlateHash") == plateHash).AnyAsync()
                : Task.FromResult(false);

            var byVinTask = vinHash is not null
                ? q.Where(v => EF.Property<string>(v, "VinHash") == vinHash).AnyAsync()
                : Task.FromResult(false);

            return await byPlateTask || await byVinTask;
        }

        // ---------- helpers (mirror AxomateDbContext) ----------
        private static string? NormUpper(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim().ToUpperInvariant();
        }

        private static string? HashOrNull(string? normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized)) return null;
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString(); // 64 hex chars
        }
    }
}

using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.Domain.Models;
using Axomate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database.Repositories
{
    public class MileageHistoryRepository : IMileageHistoryRepository
    {
        private readonly AxomateDbContext _db;
        public MileageHistoryRepository(AxomateDbContext db) => _db = db;

        // Create (returns inserted Id)
        public async Task<int> AddAsync(MileageHistory entry)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));
            if (entry.VehicleId <= 0) throw new ArgumentException("VehicleId must be a persisted ID.", nameof(entry));
            if (entry.Mileage < 0) throw new ArgumentException("Mileage must be non-negative.", nameof(entry));

            _db.MileageHistories.Add(entry);
            await _db.SaveChangesAsync();
            return entry.Id;
        }

        // Read: all records for a vehicle (newest first)
        public async Task<List<MileageHistory>> GetByVehicleAsync(int vehicleId)
        {
            return await _db.MileageHistories
                .AsNoTracking()
                .Where(m => m.VehicleId == vehicleId)
                .OrderByDescending(m => m.RecordedDate)
                .ThenByDescending(m => m.Id)
                .ToListAsync();
        }

        // Read: latest mileage on/before a given reference date
        public async Task<int?> GetLatestMileageOnOrBeforeAsync(int vehicleId, DateTime referenceDate)
        {
            return await _db.MileageHistories
                .AsNoTracking()
                .Where(m => m.VehicleId == vehicleId && m.RecordedDate <= referenceDate)
                .OrderByDescending(m => m.RecordedDate)
                .ThenByDescending(m => m.Id) // tie-break
                .Select(m => (int?)m.Mileage)
                .FirstOrDefaultAsync();
        }

        // Read: latest recorded timestamp on/before a given reference date
        public async Task<DateTime?> GetLatestRecordedAtOnOrBeforeAsync(int vehicleId, DateTime referenceDate)
        {
            return await _db.MileageHistories
                .AsNoTracking()
                .Where(m => m.VehicleId == vehicleId && m.RecordedDate <= referenceDate)
                .OrderByDescending(m => m.RecordedDate)
                .ThenByDescending(m => m.Id)
                .Select(m => (DateTime?)m.RecordedDate)
                .FirstOrDefaultAsync();
        }

        // Optional corrections

        public async Task<MileageHistory> UpdateAsync(MileageHistory entry)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));
            if (entry.Id <= 0) throw new ArgumentException("Entry must have a valid Id.", nameof(entry));

            _db.MileageHistories.Update(entry);
            await _db.SaveChangesAsync();
            return entry;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.MileageHistories.FindAsync(id);
            if (entity != null)
            {
                _db.MileageHistories.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}

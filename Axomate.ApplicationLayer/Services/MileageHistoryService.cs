using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Services
{
    public class MileageHistoryService : IMileageHistoryService
    {
        private readonly IMileageHistoryRepository _repo;

        public MileageHistoryService(IMileageHistoryRepository repo)
            => _repo = repo;

        public Task<int?> GetLatestOnOrBeforeAsync(int vehicleId, DateTime referenceDate)
            => _repo.GetLatestMileageOnOrBeforeAsync(vehicleId, referenceDate);

        public Task<int?> GetLatestForDayAsync(int vehicleId, DateTime dayLocal)
        {
            var endOfDay = dayLocal.Date.AddDays(1).AddTicks(-1);
            return _repo.GetLatestMileageOnOrBeforeAsync(vehicleId, endOfDay);
        }

        public async Task<int> RecordAsync(int vehicleId, int mileage, DateTime recordedAt, string? source, string? notes)
        {
            if (mileage < 0 || mileage > 2_000_000)
                throw new ArgumentOutOfRangeException(nameof(mileage), "Mileage must be between 0 and 2,000,000.");

            // Optional: check for regression
            var latest = await _repo.GetLatestMileageOnOrBeforeAsync(vehicleId, recordedAt);
            if (latest.HasValue && mileage < latest.Value)
                throw new InvalidOperationException("Mileage cannot be less than last recorded value.");

            var entry = new MileageHistory
            {
                VehicleId = vehicleId,
                Mileage = mileage,
                RecordedDate = recordedAt,
                Source = source,
                Notes = notes
            };

            return await _repo.AddAsync(entry);
        }

        public Task<DateTime?> GetLastRecordTimeAsync(int vehicleId, DateTime referenceDate)
            => _repo.GetLatestRecordedAtOnOrBeforeAsync(vehicleId, referenceDate);
    }
}

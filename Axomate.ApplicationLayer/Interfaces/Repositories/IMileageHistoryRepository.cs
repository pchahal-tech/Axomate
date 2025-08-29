using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Repositories
{
    public interface IMileageHistoryRepository
    {
        // Create
        Task<int> AddAsync(MileageHistory entry); // return inserted Id

        // Read
        Task<List<MileageHistory>> GetByVehicleAsync(int vehicleId);
        Task<int?> GetLatestMileageOnOrBeforeAsync(int vehicleId, DateTime referenceDate);
        Task<DateTime?> GetLatestRecordedAtOnOrBeforeAsync(int vehicleId, DateTime referenceDate);

        // Optional corrections
        Task<MileageHistory> UpdateAsync(MileageHistory entry);
        Task DeleteAsync(int id);
    }
}

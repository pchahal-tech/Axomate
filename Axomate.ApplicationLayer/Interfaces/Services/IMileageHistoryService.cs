using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Services
{
    public interface IMileageHistoryService
    {
        Task<int?> GetLatestOnOrBeforeAsync(int vehicleId, DateTime referenceDate);
        Task<int?> GetLatestForDayAsync(int vehicleId, DateTime dayLocal);
        Task<int> RecordAsync(int vehicleId, int mileage, DateTime recordedAt, string? source, string? notes);
        Task<DateTime?> GetLastRecordTimeAsync(int vehicleId, DateTime referenceDate);
    }

}

// File: Axomate.ApplicationLayer.Interfaces.Repositories/IVehicleRepository.cs
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Repositories
{
    public interface IVehicleRepository
    {
        // Create
        Task<int> AddAsync(Vehicle vehicle);

        // Update
        Task<Vehicle> UpdateAsync(Vehicle vehicle);

        // Delete
        Task DeleteAsync(int id);

        // Read
        Task<Vehicle?> GetByIdAsync(int id, bool includeCustomer = true);
        Task<List<Vehicle>> GetByCustomerAsync(int customerId);
        Task<List<Vehicle>> GetAllAsync();

        /// <summary>
        /// Checks for an existing Vehicle with the same License Plate or VIN.
        /// Uses normalized SHA-256 hash comparisons against shadow columns (LicensePlateHash/VinHash),
        /// so it works even when plaintext columns are encrypted.
        /// </summary>
        /// <param name="licensePlate">May be null or empty.</param>
        /// <param name="vin">May be null or empty.</param>
        /// <param name="excludeVehicleId">Exclude a specific Vehicle Id (for updates).</param>
        Task<bool> ExistsByPlateOrVinAsync(string licensePlate, string? vin, int? excludeVehicleId = null);
    }
}

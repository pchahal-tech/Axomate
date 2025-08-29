using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Services
{
    public interface IVehicleService
    {
        Task<List<Vehicle>> GetAllAsync();
        Task<Vehicle?> GetByIdAsync(int id);
        Task<List<Vehicle>> GetByCustomerIdAsync(int customerId);
        Task<Vehicle> AddAsync(Vehicle vehicle);
        Task<Vehicle> UpdateAsync(Vehicle vehicle);
        Task<Vehicle> AddOrUpdateAsync(Vehicle vehicle);
        Task<bool> ExistsByPlateOrVinAsync(string licensePlate, string? vin);
    }
}

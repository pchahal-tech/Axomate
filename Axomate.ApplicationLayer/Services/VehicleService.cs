using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;

        private static void Normalize(Vehicle v)
        {
            v.LicensePlate = string.IsNullOrWhiteSpace(v.LicensePlate)
                ? null
                : v.LicensePlate.Trim(); // or .ToUpperInvariant() if you prefer storing uppercase

            v.VIN = string.IsNullOrWhiteSpace(v.VIN)
                ? null
                : v.VIN.Trim(); // or .ToUpperInvariant()
        }

        public VehicleService(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        public async Task<List<Vehicle>> GetAllAsync()
        {
            return await _vehicleRepository.GetAllAsync();
        }

        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            return await _vehicleRepository.GetByIdAsync(id);
        }

        public async Task<List<Vehicle>> GetByCustomerIdAsync(int customerId)
        {
            return await _vehicleRepository.GetByCustomerAsync(customerId);
        }

        public async Task<Vehicle> AddAsync(Vehicle vehicle)
        {
            Normalize(vehicle);

            if (await ExistsByPlateOrVinAsync(vehicle.LicensePlate, vehicle.VIN))
                throw new InvalidOperationException("A vehicle with the same License Plate or VIN already exists.");

            var id = await _vehicleRepository.AddAsync(vehicle);
            vehicle.Id = id;
            return vehicle;
        }

        public async Task<Vehicle> UpdateAsync(Vehicle vehicle)
        {
            Normalize(vehicle);

            if (await _vehicleRepository.ExistsByPlateOrVinAsync(vehicle.LicensePlate, vehicle.VIN, vehicle.Id))
                throw new InvalidOperationException("Another vehicle with the same License Plate or VIN already exists.");

            return await _vehicleRepository.UpdateAsync(vehicle);
        }

        public async Task<Vehicle> AddOrUpdateAsync(Vehicle vehicle)
        {
            if (vehicle == null)
                throw new ArgumentNullException(nameof(vehicle));

            if (vehicle.Id > 0)
            {
                return await UpdateAsync(vehicle);
            }

            return await AddAsync(vehicle);
        }

        public async Task<bool> ExistsByPlateOrVinAsync(string licensePlate, string? vin)
        {
            return await _vehicleRepository.ExistsByPlateOrVinAsync(licensePlate, vin);
        }
    }
}

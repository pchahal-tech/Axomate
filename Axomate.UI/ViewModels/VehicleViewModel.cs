using Axomate.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Axomate.UI.ViewModels
{
    public partial class VehicleViewModel : ObservableObject
    {
        [ObservableProperty]
        private string make = string.Empty;

        [ObservableProperty]
        private string model = string.Empty;

        [ObservableProperty]
        private int? year = null;

        [ObservableProperty]
        private string licensePlate = string.Empty;

        [ObservableProperty]
        private string vin = string.Empty;

        [ObservableProperty]
        private string color = string.Empty;

        [ObservableProperty]
        private string engine = string.Empty;

        [ObservableProperty]
        private string transmission = string.Empty;

        [ObservableProperty]
        private string fuelType = string.Empty;

        [ObservableProperty]
        private DateTime serviceDate = DateTime.Now;

        public Vehicle ToVehicle(int customerId) => new Vehicle
        {
            Make = make,
            Model = model,
            Year = year,
            LicensePlate = licensePlate,
            VIN = vin,
            Color = color,
            Engine = engine,
            Transmission = transmission,
            FuelType = fuelType,
            CustomerId = customerId
        };

        public void LoadFromVehicle(Vehicle vehicle)
        {
            make = vehicle.Make;
            model = vehicle.Model;
            year = vehicle.Year;
            licensePlate = vehicle.LicensePlate;
            vin = vehicle.VIN;
            color = vehicle.Color;
            engine = vehicle.Engine;
            transmission = vehicle.Transmission;
            fuelType = vehicle.FuelType;
        }
    }
}

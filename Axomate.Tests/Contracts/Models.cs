using System;
using System.Collections.Generic;

namespace Axomate.Tests.Contracts
{
    public record Customer
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? AddressLine1 { get; init; }
        public string? Phone { get; init; }
        public string? Email { get; init; }
    }

    public record Vehicle
    {
        public int Id { get; init; }
        public int CustomerId { get; init; }
        public string? Make { get; init; }
        public string? Model { get; init; }
        public string? VIN { get; init; }
        public string? LicensePlate { get; init; }
        public string? Transmission { get; init; }
        public string? FuelType { get; init; }
        public string? Color { get; init; }
        public string? Engine { get; init; }
        public int? Year { get; init; }
    }

    public record LineItem
    {
        public int? ServiceItemId { get; init; }
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int Quantity { get; init; } = 1;
    }

    public record Invoice
    {
        public int Id { get; init; }
        public int CustomerId { get; init; }
        public int VehicleId { get; init; }
        public DateTime ServiceDate { get; init; }
        public int? Mileage { get; init; }
        public IReadOnlyList<LineItem> LineItems { get; init; } = Array.Empty<LineItem>();
    }
}

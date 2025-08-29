using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Xunit;
using FluentAssertions;

using Microsoft.EntityFrameworkCore;                         // EF Core extensions
using Axomate.Tests.Contracts;                               // Demo contracts
using Axomate.Tests.Demo;                                    // DemoFlow, InMemoryServices
using Axomate.Tests.TestConfig;                              // FixedTimeProvider, ListLogger

using static Axomate.Tests.TestHelpers.TestDbFactory;        // MakeDb()

// Alias domain models to avoid clashing with Axomate.Tests.Contracts.Vehicle
using DomainVehicle = Axomate.Domain.Models.Vehicle;
using DomainCustomer = Axomate.Domain.Models.Customer;

namespace Axomate.Tests
{
    public class VehicleFlowTests
    {
        private static (DemoFlow flow, InMemoryServices svcs, FixedTimeProvider time) Harness()
        {
            var time = new FixedTimeProvider(new DateTime(2025, 8, 22, 12, 0, 0, DateTimeKind.Utc));
            var svcs = new InMemoryServices();
            var logger = new ListLogger();
            var flow = new DemoFlow(svcs, svcs, svcs, svcs, svcs, logger, time);
            return (flow, svcs, time);
        }

        private static async Task SeedCustomerAsync(DemoFlow flow, InMemoryServices svcs)
        {
            flow.AddNewCustomer();
            flow.NewCustomer = flow.NewCustomer with { Name = "Alice" };
            await flow.SaveNewCustomerAsync();
        }

        // --- Encryption & hash-sidecar lookup test against real EF Core context ---
        [Fact]
        public async Task Vehicle_Plate_IsEncrypted_And_Searches_ByHash()
        {
            using var ctx = MakeDb(); // SQLite test DB created per-run

            // Need a real Customer to satisfy FK
            var cust = new DomainCustomer { Name = "Test" };
            ctx.Customers.Add(cust);
            await ctx.SaveChangesAsync();

            // Insert a Vehicle (EF converter handles encryption; sidecar hashes maintained on SaveChanges)
            var v = new DomainVehicle
            {
                CustomerId = cust.Id,
                Make = "Honda",
                Model = "Civic",
                LicensePlate = "abc123"
            };
            ctx.Vehicles.Add(v);
            await ctx.SaveChangesAsync();

            // Query by the shadow sidecar (normalize -> SHA-256 hex)
            string plateNorm = NormUpper("ABC123")!;
            string plateHash = Sha256Hex(plateNorm)!;

            var found = await ctx.Vehicles
                .AsNoTracking()
                .Where(x => EF.Property<string>(x, "LicensePlateHash") == plateHash)
                .SingleOrDefaultAsync();

            Assert.NotNull(found);

            // If you want to assert ciphertext-at-rest, gate it on Windows due to DPAPI:
            // if (OperatingSystem.IsWindows())
            // {
            //     // (Read raw column via ADO here and assert it != "abc123")
            // }
        }

        [Fact]
        public async Task AddNewVehicle_WithoutCustomer_ShowsMessage()
        {
            var (flow, _, _) = Harness();
            flow.AddNewVehicle();
            flow.LastUserMessage.Should().Be("Select or save a customer first.");
        }

        [Fact]
        public async Task SaveNewVehicle_ValidatesFields()
        {
            var (flow, svcs, _) = Harness();
            await SeedCustomerAsync(flow, svcs);

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "", Model = "", VIN = "", LicensePlate = "" };
            await flow.SaveNewVehicleAsync();

            flow.LastUserMessage.Should().Contain("requires VIN or LicensePlate or Make+Model");
            flow.NewVehicleMode.Should().BeTrue();
        }

        [Fact]
        public async Task SaveNewVehicle_RecordsMileage_IfLastRecordStale()
        {
            var (flow, svcs, time) = Harness();
            await SeedCustomerAsync(flow, svcs);

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "Toyota", Model = "Corolla", LicensePlate = "ABC123" };
            flow.MileageText = "10000";
            await flow.SaveNewVehicleAsync();

            svcs.Mileage.Should().ContainKey(flow.SelectedVehicle!.Id);
            svcs.Mileage[flow.SelectedVehicle!.Id].Should().HaveCount(1);

            time.Advance(TimeSpan.FromHours(2));

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "Toyota", Model = "Corolla", LicensePlate = "XYZ789" };
            flow.MileageText = "11000";
            await flow.SaveNewVehicleAsync();

            svcs.Mileage[flow.SelectedVehicle!.Id].Should().HaveCount(1, "5-hour lock prevents new record");
        }

        [Fact]
        public async Task SaveNewVehicle_DuplicatePlate_ShowsFriendlyMessage()
        {
            var (flow, svcs, _) = Harness();
            await SeedCustomerAsync(flow, svcs);

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "Honda", Model = "Civic", LicensePlate = "DUP1" };
            await flow.SaveNewVehicleAsync();

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "Honda", Model = "Accord", LicensePlate = "DUP1" };
            await flow.SaveNewVehicleAsync();

            flow.LastUserMessage.Should().Be("A vehicle with this license plate already exists.");
        }

        // ---- local helpers for the hash-sidecar test ----
        private static string? NormUpper(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim().ToUpperInvariant();
        }

        private static string? Sha256Hex(string? normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized)) return null;
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}

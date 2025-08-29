using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Axomate.Tests.Contracts;
using Axomate.Tests.Demo;
using Axomate.Tests.TestConfig;
using System;

namespace Axomate.Tests
{
    public class MileageBehaviorTests
    {
        private static (DemoFlow flow, InMemoryServices svcs, FixedTimeProvider time) Harness()
        {
            var time = new FixedTimeProvider(new DateTime(2025, 8, 22, 12, 0, 0, DateTimeKind.Utc));
            var svcs = new InMemoryServices();
            var logger = new ListLogger();
            var flow = new DemoFlow(svcs, svcs, svcs, svcs, svcs, logger, time);
            return (flow, svcs, time);
        }

        private static async Task SeedCustomerVehicleAsync(DemoFlow flow, InMemoryServices svcs)
        {
            flow.AddNewCustomer();
            flow.NewCustomer = flow.NewCustomer with { Name = "Eve" };
            await flow.SaveNewCustomerAsync();

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "Tesla", Model = "Model 3", LicensePlate = "EV1" };
            await flow.SaveNewVehicleAsync();
        }

        [Fact]
        public async Task TypingMileage_Records_WhenStale()
        {
            var (flow, svcs, time) = Harness();
            await SeedCustomerVehicleAsync(flow, svcs);

            flow.MileageText = "100";
            svcs.Mileage[flow.SelectedVehicle!.Id].Should().HaveCount(1);

            time.Advance(TimeSpan.FromHours(6));
            flow.MileageText = "200";
            svcs.Mileage[flow.SelectedVehicle!.Id].Should().HaveCount(2);
        }

        [Fact]
        public async Task TypingMileage_Within5Hours_IsBlocked_AndReverted()
        {
            var (flow, svcs, time) = Harness();
            await SeedCustomerVehicleAsync(flow, svcs);
            flow.MileageText = "1000";
            svcs.Mileage[flow.SelectedVehicle!.Id].Should().HaveCount(1);

            time.Advance(TimeSpan.FromHours(2));
            flow.MileageText = "900";
            flow.Mileage.Should().Be(1000);
            flow.LastUserMessage.Should().MatchRegex("(locked.*Reverted|Mileage cannot decrease)");
        }

        [Fact]
        public async Task SaveTimeMileage_Insert_OnlyIfNoRecordInLast5Hours()
        {
            var (flow, svcs, time) = Harness();
            await SeedCustomerVehicleAsync(flow, svcs);

            flow.CreateNewInvoice();
            flow.AddLineItem(new LineItem { Description = "Tire", Price = 10m, Quantity = 1 });
            flow.MileageText = "3000";
            await flow.SaveAndPrintInvoiceAsync();
            svcs.Mileage[flow.SelectedVehicle!.Id].Should().HaveCount(1);

            flow.CreateNewInvoice();
            flow.AddLineItem(new LineItem { Description = "Tire", Price = 10m, Quantity = 1 });
            flow.MileageText = "3500";
            await flow.SaveAndPrintInvoiceAsync();
            svcs.Mileage[flow.SelectedVehicle!.Id].Should().HaveCount(1);

            time.Advance(TimeSpan.FromHours(6));
            flow.CreateNewInvoice();
            flow.AddLineItem(new LineItem { Description = "Tire", Price = 10m, Quantity = 1 });
            flow.MileageText = "4000";
            await flow.SaveAndPrintInvoiceAsync();
            svcs.Mileage[flow.SelectedVehicle!.Id].Should().HaveCount(2);
        }
    }
}

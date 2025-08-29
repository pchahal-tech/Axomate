using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Axomate.Tests.Contracts;
using Axomate.Tests.Demo;
using Axomate.Tests.TestConfig;
using System;

namespace Axomate.Tests
{
    public class InvoiceFlowTests
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
            flow.NewCustomer = flow.NewCustomer with { Name = "Bob" };
            await flow.SaveNewCustomerAsync();

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "Ford", Model = "F150", LicensePlate = "TRK1" };
            flow.MileageText = "50000";
            await flow.SaveNewVehicleAsync();
        }

        [Fact]
        public void SaveAndPrint_DisabledUntilNewInvoice()
        {
            var (flow, _, _) = Harness();
            flow.CanSaveAndPrint.Should().BeFalse();
            flow.CreateNewInvoice();
            flow.CanSaveAndPrint.Should().BeTrue("mode on & not busy");
        }

        [Fact]
        public async Task SaveAndPrint_FailsWithEmptyItems()
        {
            var (flow, svcs, _) = Harness();
            await SeedCustomerVehicleAsync(flow, svcs);

            flow.CreateNewInvoice();
            await flow.SaveAndPrintInvoiceAsync();

            flow.LastUserMessage.Should().Contain("At least one line item");
            flow.NewInvoiceMode.Should().BeTrue("should remain enabled for retry");
        }

        [Fact]
        public async Task SaveAndPrint_SetsBusy_DisablesReentry()
        {
            var (flow, svcs, _) = Harness();
            await SeedCustomerVehicleAsync(flow, svcs);
            flow.CreateNewInvoice();
            flow.AddLineItem(new LineItem { Description = "Oil Change", Price = 49.99m, Quantity = 1 });

            var t1 = flow.SaveAndPrintInvoiceAsync();
            flow.IsBusy.Should().BeTrue();
            flow.CanSaveAndPrint.Should().BeFalse();

            await t1;
            flow.IsBusy.Should().BeFalse();
            flow.NewInvoiceMode.Should().BeFalse();
        }

        [Fact]
        public async Task SaveAndPrint_SavesInvoice_AndPdfMatchesMileageAndDate()
        {
            var (flow, svcs, time) = Harness();
            await SeedCustomerVehicleAsync(flow, svcs);

            flow.CreateNewInvoice();
            flow.AddLineItem(new LineItem { Description = "Alignment", Price = 100m, Quantity = 1 });
            flow.MileageText = "50500";
            await flow.SaveAndPrintInvoiceAsync();

            svcs.Invoices.Should().HaveCount(1);
            var pdf = svcs.Snapshot();
            pdf.last.Should().NotBeNull();
            pdf.last!.Mileage.Should().Be(flow.Mileage);
            pdf.last!.ServiceDate.Date.Should().Be(flow.ServiceDate.Date);
        }

        [Fact]
        public async Task SaveAndPrint_DuplicateInvoice_ShowsFriendlyMessage()
        {
            var (flow, svcs, _) = Harness();
            await SeedCustomerVehicleAsync(flow, svcs);

            flow.CreateNewInvoice();
            flow.AddLineItem(new LineItem { Description = "Wash", Price = 20m, Quantity = 1 });
            await flow.SaveAndPrintInvoiceAsync();

            flow.CreateNewInvoice();
            flow.AddLineItem(new LineItem { Description = "Wash", Price = 25m, Quantity = 1 });
            await flow.SaveAndPrintInvoiceAsync();

            flow.LastUserMessage.Should().Be("An invoice for this customer/vehicle/date already exists.");
        }
    }
}

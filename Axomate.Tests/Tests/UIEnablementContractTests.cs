using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Axomate.Tests.Contracts;
using Axomate.Tests.Demo;
using Axomate.Tests.TestConfig;
using System;

namespace Axomate.Tests
{
    public class UIEnablementContractTests
    {
        private static (DemoFlow flow, InMemoryServices svcs, FixedTimeProvider time) Harness()
        {
            var time = new FixedTimeProvider(new DateTime(2025, 8, 22, 12, 0, 0, DateTimeKind.Utc));
            var svcs = new InMemoryServices();
            var logger = new ListLogger();
            var flow = new DemoFlow(svcs, svcs, svcs, svcs, svcs, logger, time);
            return (flow, svcs, time);
        }

        [Fact]
        public void NewInvoice_Enablement_Follows_Mode_And_IsBusy()
        {
            var (flow, _, _) = Harness();

            flow.CanSaveAndPrint.Should().BeFalse();
            flow.CreateNewInvoice();
            flow.CanSaveAndPrint.Should().BeTrue();
        }

        [Fact]
        public async Task SaveAndPrint_Disables_After_Success()
        {
            var (flow, svcs, _) = Harness();

            flow.AddNewCustomer();
            flow.NewCustomer = flow.NewCustomer with { Name = "X" };
            await flow.SaveNewCustomerAsync();

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "Y", Model = "Z", LicensePlate = "P1" };
            await flow.SaveNewVehicleAsync();

            flow.CreateNewInvoice();
            flow.AddLineItem(new LineItem { Description = "Item", Price = 1m, Quantity = 1 });
            await flow.SaveAndPrintInvoiceAsync();

            flow.NewInvoiceMode.Should().BeFalse();
            flow.CanSaveAndPrint.Should().BeFalse();
        }
    }
}

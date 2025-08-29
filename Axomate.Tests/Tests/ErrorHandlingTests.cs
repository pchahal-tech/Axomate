using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Axomate.Tests.Contracts;
using Axomate.Tests.Demo;
using Axomate.Tests.TestConfig;
using System;

namespace Axomate.Tests
{
    public class ErrorHandlingTests
    {
        private static (DemoFlow flow, InMemoryServices svcs, FixedTimeProvider time, ListLogger log) Harness()
        {
            var time = new FixedTimeProvider(new DateTime(2025, 8, 22, 12, 0, 0, DateTimeKind.Utc));
            var svcs = new InMemoryServices();
            var logger = new ListLogger();
            var flow = new DemoFlow(svcs, svcs, svcs, svcs, svcs, logger, time);
            return (flow, svcs, time, logger);
        }

        [Fact]
        public async Task DuplicateVehicle_IsLogged_And_UserNotified()
        {
            var (flow, svcs, _, log) = Harness();

            flow.AddNewCustomer();
            flow.NewCustomer = flow.NewCustomer with { Name = "User" };
            await flow.SaveNewCustomerAsync();

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "A", Model = "B", LicensePlate = "Q1" };
            await flow.SaveNewVehicleAsync();

            flow.AddNewVehicle();
            flow.Vehicle = flow.Vehicle with { Make = "A", Model = "C", LicensePlate = "Q1" };
            await flow.SaveNewVehicleAsync();

            log.Entries.Should().NotBeEmpty();
            flow.LastUserMessage.Should().Be("A vehicle with this license plate already exists.");
        }
    }
}

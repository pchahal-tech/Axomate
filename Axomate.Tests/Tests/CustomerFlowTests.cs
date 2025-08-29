using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Axomate.Tests.Contracts;
using Axomate.Tests.Demo;
using Axomate.Tests.TestConfig;
using System;

namespace Axomate.Tests
{
    public class CustomerFlowTests
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
        public async Task AddNewCustomer_Save_SetsSelection_DisablesMode()
        {
            var (flow, svcs, _) = Harness();

            flow.AddNewCustomer();
            flow.NewCustomerMode.Should().BeTrue();
            flow.CanSaveNewCustomer.Should().BeTrue();

            flow.NewCustomer = flow.NewCustomer with { Name = "  John  " };
            await flow.SaveNewCustomerAsync();

            flow.NewCustomerMode.Should().BeFalse();
            flow.SelectedCustomer.Should().NotBeNull();
            svcs.Customers.Should().HaveCount(1);
        }

        [Fact]
        public async Task SaveNewCustomer_WithoutName_ShowsMessage()
        {
            var (flow, _, _) = Harness();

            flow.AddNewCustomer();
            flow.NewCustomer = flow.NewCustomer with { Name = "" };
            await flow.SaveNewCustomerAsync();

            flow.LastUserMessage.Should().NotBeNullOrWhiteSpace();
            flow.NewCustomerMode.Should().BeTrue("save failed, remain in mode for retry");
        }
    }
}

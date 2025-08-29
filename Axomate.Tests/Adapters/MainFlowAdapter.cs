using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Axomate.Tests.Contracts;

namespace Axomate.Tests.Adapters
{
#if !USE_DEMO_FLOW
    /// <summary>
    /// Implement this by wrapping your real MainViewModel.
    /// </summary>
    public sealed class MainFlowAdapter : IMainFlow
    {
        public bool NewCustomerMode => throw new NotImplementedException();
        public bool NewVehicleMode => throw new NotImplementedException();
        public bool NewInvoiceMode => throw new NotImplementedException();
        public bool IsBusy => throw new NotImplementedException();

        public Customer? SelectedCustomer => throw new NotImplementedException();
        public Vehicle? SelectedVehicle => throw new NotImplementedException();
        public Customer NewCustomer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Vehicle Vehicle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string? MileageText { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int? Mileage => throw new NotImplementedException();
        public DateTime ServiceDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<LineItem> LineItems => throw new NotImplementedException();
        public void AddLineItem(LineItem item) => throw new NotImplementedException();
        public void ClearLineItems() => throw new NotImplementedException();

        public void AddNewCustomer() => throw new NotImplementedException();
        public Task SaveNewCustomerAsync() => throw new NotImplementedException();

        public void AddNewVehicle() => throw new NotImplementedException();
        public Task SaveNewVehicleAsync() => throw new NotImplementedException();

        public void CreateNewInvoice() => throw new NotImplementedException();
        public Task SaveAndPrintInvoiceAsync() => throw new NotImplementedException();

        public void ResetForm() => throw new NotImplementedException();

        public bool CanSaveNewCustomer => throw new NotImplementedException();
        public bool CanSaveNewVehicle => throw new NotImplementedException();
        public bool CanSaveAndPrint => throw new NotImplementedException();

        public string? LastUserMessage => throw new NotImplementedException();
    }
#endif
}

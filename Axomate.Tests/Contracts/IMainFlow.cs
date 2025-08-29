using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Axomate.Tests.Contracts;

namespace Axomate.Tests.Contracts
{
    /// <summary>
    /// Contract that the tests exercise. Your MainViewModel should be adapted to this contract.
    /// </summary>
    public interface IMainFlow
    {
        // Modes
        bool NewCustomerMode { get; }
        bool NewVehicleMode { get; }
        bool NewInvoiceMode { get; }
        bool IsBusy { get; }

        // Selection & buffers
        Customer? SelectedCustomer { get; }
        Vehicle? SelectedVehicle { get; }
        Customer NewCustomer { get; set; }      // editable snapshot
        Vehicle Vehicle { get; set; }           // editable snapshot

        // Mileage
        string? MileageText { get; set; }
        int? Mileage { get; }
        DateTime ServiceDate { get; set; }     // date part used

        // Line Items
        IList<LineItem> LineItems { get; }
        void AddLineItem(LineItem item);
        void ClearLineItems();

        // Commands
        void AddNewCustomer();
        Task SaveNewCustomerAsync();

        void AddNewVehicle();
        Task SaveNewVehicleAsync();

        void CreateNewInvoice();
        Task SaveAndPrintInvoiceAsync();

        void ResetForm();

        // Derived enablement (logical contract)
        bool CanSaveNewCustomer { get; }
        bool CanSaveNewVehicle { get; }
        bool CanSaveAndPrint { get; }

        // User feedback
        string? LastUserMessage { get; }
    }
}

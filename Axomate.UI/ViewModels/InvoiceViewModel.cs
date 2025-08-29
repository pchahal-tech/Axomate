using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace Axomate.UI.ViewModels
{
    public partial class InvoiceViewModel : ObservableObject
    {
        private readonly ICustomerService _customerService;
        private readonly IVehicleService _vehicleService;
        private readonly IInvoiceService _invoiceService;
        private readonly IMileageHistoryService _mileageHistoryService;

        public InvoiceViewModel(
            ICustomerService customerService,
            IVehicleService vehicleService,
            IInvoiceService invoiceService,
            IMileageHistoryService mileageHistoryService)
        {
            _customerService = customerService;
            _vehicleService = vehicleService;
            _invoiceService = invoiceService;
            _mileageHistoryService = mileageHistoryService;

            InvoiceItems = new ObservableCollection<ServiceItemRow>();
            ServiceDate = DateTime.Now;
        }

        [ObservableProperty] private Customer selectedCustomer;
        [ObservableProperty] private Vehicle selectedVehicle;
        [ObservableProperty] private ServiceItem selectedServiceItem;
        [ObservableProperty] private ObservableCollection<ServiceItemRow> invoiceItems;

        [ObservableProperty] private int? mileage;
        [ObservableProperty] private DateTime serviceDate;

        // Example: Add a service row
        [RelayCommand]
        private void AddServiceItem()
        {
            if (SelectedServiceItem == null) return;

            var isOther = string.Equals(SelectedServiceItem.Name, "Other", StringComparison.OrdinalIgnoreCase);

            if (!isOther && InvoiceItems.Any(r => r.Id == SelectedServiceItem.Id))
            {
                MessageBox.Show("This service has already been added.");
                return;
            }

            if (isOther)
            {
                InvoiceItems.Add(new ServiceItemRow { Name = "", Price = 0m });
            }
            else
            {
                InvoiceItems.Add(new ServiceItemRow
                {
                    Id = SelectedServiceItem.Id,
                    Name = SelectedServiceItem.Name,
                    Price = SelectedServiceItem.Price,
                    Quantity = 1
                });
            }
        }

        private bool ValidateInputs(out string error)
        {
            if (SelectedCustomer == null || string.IsNullOrWhiteSpace(SelectedCustomer.Name))
            {
                error = "Customer name is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(SelectedCustomer.Email) ||
                !Regex.IsMatch(SelectedCustomer.Email, @"^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$"))
            {
                error = "A valid customer email is required.";
                return false;
            }

            if (SelectedVehicle == null ||
                string.IsNullOrWhiteSpace(SelectedVehicle.Make) ||
                string.IsNullOrWhiteSpace(SelectedVehicle.Model) ||
                string.IsNullOrWhiteSpace(SelectedVehicle.LicensePlate))
            {
                error = "Complete vehicle information is required.";
                return false;
            }

            if (InvoiceItems == null || InvoiceItems.Count == 0)
            {
                error = "At least one service item is required.";
                return false;
            }

            if (InvoiceItems.Any(i => i.Price <= 0 || i.Quantity <= 0))
            {
                error = "Service item prices and quantities must be greater than zero.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        // Example: Build invoice object
        private Invoice BuildInvoiceForSave()
        {
            return new Invoice
            {
                CustomerId = SelectedCustomer.Id,
                VehicleId = SelectedVehicle.Id,
                ServiceDate = ServiceDate,
                Mileage = Mileage,
                LineItems = InvoiceItems.Select(r => new InvoiceLineItem
                {
                    ServiceItemId = r.Id,
                    Description = string.IsNullOrWhiteSpace(r.Name) ? "Other" : r.Name,
                    Price = r.Price,
                    Quantity = r.Quantity
                }).ToList()
            };
        }
    }
}

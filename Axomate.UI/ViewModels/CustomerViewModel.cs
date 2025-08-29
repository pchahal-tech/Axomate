using Axomate.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Axomate.UI.ViewModels
{
    public partial class CustomerViewModel : ObservableObject
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string? addressLine1;

        [ObservableProperty]
        private string? phone;

        [ObservableProperty]
        private string? email;

        [ObservableProperty]
        private bool isEditing;

        public CustomerViewModel() { }

        public CustomerViewModel(Customer customer)
        {
            LoadFromModel(customer);
        }

        public void LoadFromModel(Customer customer)
        {
            Id = customer.Id;
            Name = customer.Name;
            AddressLine1 = customer.AddressLine1;
            Phone = customer.Phone;
            Email = customer.Email;
        }

        public Customer ToModel()
        {
            return new Customer
            {
                Id = this.Id,
                Name = this.Name,
                AddressLine1 = this.AddressLine1,
                Phone = this.Phone,
                Email = this.Email
            };
        }

        [RelayCommand]
        private void BeginEdit()
        {
            IsEditing = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
        }
    }
}

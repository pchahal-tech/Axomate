using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Axomate.UI.ViewModels
{
    public partial class CompanyViewModel : ObservableObject
    {
        private readonly ICompanyService _service;

        [ObservableProperty]
        private Company company = new();

        public CompanyViewModel(ICompanyService service)
        {
            _service = service;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var loaded = await _service.GetAsync();
            Company = loaded ?? new Company();
        }
    }
}

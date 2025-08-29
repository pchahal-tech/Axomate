using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.ApplicationLayer.Interfaces.Services;
using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _repository;

        public CompanyService(ICompanyRepository repository)
        {
            _repository = repository;
        }

        public async Task<Company?> GetAsync()
        {
            return await _repository.GetAsync();
        }

        public async Task<Company> UpdateAsync(Company company)
        {
            // Example validation
            if (string.IsNullOrWhiteSpace(company.Name))
                throw new ArgumentException("Company name is required.");

            if (company.GstRate < 0 || company.GstRate > 1)
                throw new ArgumentOutOfRangeException(nameof(company.GstRate), "GST must be between 0 and 1.");

            if (company.PstRate < 0 || company.PstRate > 1)
                throw new ArgumentOutOfRangeException(nameof(company.PstRate), "PST must be between 0 and 1.");

            await _repository.UpdateAsync(company);
            return company;
        }
    }
}

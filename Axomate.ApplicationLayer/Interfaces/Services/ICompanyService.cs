using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Services
{
    public interface ICompanyService
    {
        Task<Company?> GetAsync();
        Task<Company> UpdateAsync(Company company);
    }

}

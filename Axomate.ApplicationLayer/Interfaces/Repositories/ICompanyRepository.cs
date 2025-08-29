using Axomate.Domain.Models;

namespace Axomate.ApplicationLayer.Interfaces.Repositories
{
    public interface ICompanyRepository
    {
        Task<Company?> GetAsync();
        Task<Company> UpdateAsync(Company company);
    }
}

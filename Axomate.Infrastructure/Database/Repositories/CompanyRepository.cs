using Axomate.ApplicationLayer.Interfaces.Repositories;
using Axomate.Domain.Models;
using Axomate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly AxomateDbContext _dbContext;

        public CompanyRepository(AxomateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Company?> GetAsync()
        {
            return await _dbContext.Companies.FirstOrDefaultAsync();
        }

        public async Task<Company> UpdateAsync(Company company)
        {
            var existing = await _dbContext.Companies.FirstOrDefaultAsync();

            if (existing is null)
            {
                _dbContext.Companies.Add(company);
                await _dbContext.SaveChangesAsync();
                return company;
            }
            else
            {
                _dbContext.Entry(existing).CurrentValues.SetValues(company);
                await _dbContext.SaveChangesAsync();
                return existing;
            }
        }
    }
}

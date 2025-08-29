using Axomate.Infrastructure.Database.Repositories;

namespace Axomate.Infrastructure.Database.Seeders
{
    public class SampleDataSeeder
    {
        private readonly ServiceItemSeeder _serviceItemSeeder;
        private readonly CompanySeeder _companySeeder;

        public SampleDataSeeder(ServiceItemSeeder serviceItemSeeder, CompanySeeder companySeeder)
        {
            _serviceItemSeeder = serviceItemSeeder;
            _companySeeder = companySeeder;
        }

        public async Task SeedAsync()
        {
            await _serviceItemSeeder.SeedAsync();
            await _companySeeder.SeedAsync();
        }
    }
}

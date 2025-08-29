using Axomate.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database.Seeders
{
    public class CompanySeeder
    {
        private readonly AxomateDbContext _dbContext;

        public CompanySeeder(AxomateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SeedAsync()
        {
            var existing = await _dbContext.Companies.FirstOrDefaultAsync();

            if (existing == null)
            {
                var company = new Company
                {
                    Name = "HP CAR FIX LTD.",
                    Tagline = "Smart Automation Solutions",
                    AddressLine1 = "13412 72 Ave",
                    AddressLine2 = "Surrey V3W 2N8",
                    Phone1 = "778-922-6041",
                    Phone2 = "604-772-2330",
                    Email = "hpmobiletire@gmail.com",
                    Website = "https://www.hpcarfix.ca", 
                    GstNumber = "XXXXX",
                    ReviewQrText = "Thank you for your business!",
                    LogoPath = "Axomate_LogoOnly_512x512.png",
                    GstRate = 0.05m,
                    PstRate = 0.00m
                };

                _dbContext.Companies.Add(company);
            }
            else
            {
                // keep in sync with Project Notes values
                existing.Name = "HP CAR FIX LTD.";
                existing.Tagline = "Smart Automation Solutions";
                existing.AddressLine1 = "13412 72 Ave";
                existing.AddressLine2 = "Surrey V3W 2N8";
                existing.Phone1 = "778-922-6041";
                existing.Phone2 = "604-772-2330";
                existing.Email = "hpmobiletire@gmail.com";
                existing.Website = "https://www.hpcarfix.ca"; 
                existing.GstNumber = "XXXXX";
                existing.ReviewQrText = "Thank you for your business!";
                existing.LogoPath = "/Assets/Images/CompanyLogo.png";
                existing.GstRate = 0.05m;
                existing.PstRate = 0.00m;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}

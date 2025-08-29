using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Infrastructure.Database
{
    /// <summary>
    /// One-time/idempotent backfill that forces EF to re-save Company.GstNumber
    /// through the encryption converter, without touching other columns.
    /// Safe to run at startup after migrations.
    /// </summary>
    public static class CompanyGstBackfill
    {
        public static async Task RunAsync(AxomateDbContext db)
        {
            // Pull all companies (usually few)
            var companies = await db.Companies.AsNoTracking().ToListAsync();

            foreach (var c in companies)
            {
                // Attach a stub so we only update the GstNumber column
                db.Companies.Attach(c);
                // Mark ONLY GstNumber as modified so it writes via the converter
                db.Entry(c).Property(nameof(c.GstNumber)).IsModified = true;
            }

            if (companies.Count > 0)
                await db.SaveChangesAsync();
        }
    }
}

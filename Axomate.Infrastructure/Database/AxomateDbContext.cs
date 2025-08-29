using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Axomate.Domain.Models;
using Axomate.Infrastructure.Database.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Axomate.Infrastructure.Database
{
    public class AxomateDbContext : DbContext
    {
        public AxomateDbContext(DbContextOptions<AxomateDbContext> options)
            : base(options) { }

        // DbSets
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
        public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<MileageHistory> MileageHistories => Set<MileageHistory>();
        public DbSet<AdminCredential> AdminCredentials => Set<AdminCredential>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------- SQLite decimal handling --------
            if (Database.IsSqlite())
            {
                var decConverter = new ValueConverter<decimal, double>(
                    v => Convert.ToDouble(v),
                    v => Convert.ToDecimal(v)
                );
                var decNullConverter = new ValueConverter<decimal?, double?>(
                    v => v.HasValue ? Convert.ToDouble(v.Value) : (double?)null,
                    v => v.HasValue ? Convert.ToDecimal(v.Value) : (decimal?)null
                );

                foreach (var prop in modelBuilder.Model
                             .GetEntityTypes()
                             .SelectMany(t => t.GetProperties())
                             .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
                {
                    prop.SetValueConverter(prop.IsNullable ? decNullConverter : decConverter);
                }
            }

            // -------- DPAPI string encryption converter --------
            var enc = new EncryptedStringConverter();

            modelBuilder.Entity<AdminCredential>(b =>
            {
                b.HasIndex(x => x.Username).IsUnique();
            });

            // ----------------------- Customer -----------------------
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AddressLine1).HasMaxLength(200);

                // Encrypt PII
                entity.Property(e => e.Phone).HasConversion(enc).HasMaxLength(20);
                entity.Property(e => e.Email).HasConversion(enc).HasMaxLength(100);

                // Shadow sidecar hashes for search/index (non-unique)
                entity.Property<string>("PhoneHash").HasMaxLength(64);
                entity.Property<string>("EmailHash").HasMaxLength(64);
                entity.HasIndex("PhoneHash");
                entity.HasIndex("EmailHash");

                entity.HasMany(e => e.Vehicles)
                      .WithOne(v => v.Customer)
                      .HasForeignKey(v => v.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Name);
            });

            // ----------------------- Vehicle -----------------------
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.Property(v => v.Make).HasMaxLength(50);
                entity.Property(v => v.Model).HasMaxLength(50);

                // Encrypt PII
                entity.Property(v => v.LicensePlate)
                      .HasMaxLength(20)
                      .IsRequired(false)
                      .HasConversion(enc);

                entity.Property(v => v.VIN)
                      .HasMaxLength(17)
                      .IsRequired(false)
                      .HasConversion(enc);

                // Shadow sidecar hashes for uniqueness & search
                entity.Property<string>("LicensePlateHash").HasMaxLength(64);
                entity.Property<string>("VinHash").HasMaxLength(64);

                // Unique only when present
                entity.HasIndex("LicensePlateHash")
                      .IsUnique()
                      .HasFilter("LicensePlateHash IS NOT NULL");

                entity.HasIndex("VinHash")
                      .IsUnique()
                      .HasFilter("VinHash IS NOT NULL");

                entity.HasOne(v => v.Customer)
                      .WithMany(c => c.Vehicles)
                      .HasForeignKey(v => v.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ----------------------- Company -----------------------
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Tagline).HasMaxLength(100);
                entity.Property(e => e.AddressLine1).HasMaxLength(200);
                entity.Property(e => e.AddressLine2).HasMaxLength(200);
                entity.Property(e => e.Phone1).HasMaxLength(20);
                entity.Property(e => e.Phone2).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Website).HasMaxLength(100);
                entity.Property(e => e.LogoPath).HasMaxLength(255);

                // 🔐 Encrypt ONLY the GST number
                entity.Property(e => e.GstNumber)
                      .HasMaxLength(30)
                      .HasConversion(enc);

                entity.Property(e => e.GstRate).HasColumnType("decimal(5,4)");
                entity.Property(e => e.PstRate).HasColumnType("decimal(5,4)");
                entity.Property(e => e.ReviewQrText).HasMaxLength(500);
            });

            // ----------------------- ServiceItem -----------------------
            modelBuilder.Entity<ServiceItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            });

            // ----------------------- Invoice -----------------------
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.HasOne(i => i.Customer)
                      .WithMany()
                      .HasForeignKey(i => i.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired();

                entity.HasOne(i => i.Vehicle)
                      .WithMany()
                      .HasForeignKey(i => i.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired();

                entity.HasMany(i => i.LineItems)
                      .WithOne(li => li.Invoice)
                      .HasForeignKey(li => li.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(i => i.ServiceDate);
                entity.HasIndex(i => new { i.CustomerId, i.VehicleId, i.ServiceDate });
            });

            // ----------------------- InvoiceLineItem -----------------------
            modelBuilder.Entity<InvoiceLineItem>(entity =>
            {
                entity.HasKey(li => li.Id);
                entity.Property(li => li.Description).IsRequired().HasMaxLength(200);
                entity.Property(li => li.Price).HasColumnType("decimal(10,2)");
                entity.Property(li => li.Quantity).IsRequired();

                entity.HasOne(li => li.Invoice)
                      .WithMany(i => i.LineItems)
                      .HasForeignKey(li => li.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .IsRequired();

                entity.HasOne(li => li.ServiceItem)
                      .WithMany()
                      .HasForeignKey(li => li.ServiceItemId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_InvoiceLineItem_Price_NonNegative", "Price >= 0");
                    tb.HasCheckConstraint("CK_InvoiceLineItem_Quantity_Positive", "Quantity >= 1");
                });
            });

            // ----------------------- MileageHistory -----------------------
            _ = modelBuilder.Entity<MileageHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Mileage).IsRequired();
                entity.Property(e => e.RecordedDate).IsRequired();

                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint(
                        "CK_MileageHistory_Mileage_Range",
                        "Mileage >= 0 AND Mileage <= 2000000");
                });

                entity.HasIndex(e => new { e.VehicleId, e.RecordedDate });

                entity.HasOne(e => e.Vehicle)
                      .WithMany(v => v.MileageHistories)
                      .HasForeignKey(e => e.VehicleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ----------------------- AUDIT: CreatedDate / CreatedBy (shadow props) -----------------------
            foreach (var et in modelBuilder.Model.GetEntityTypes()
                     .Where(t => !t.IsOwned() && !t.IsKeyless))
            {
                var entity = modelBuilder.Entity(et.ClrType);
                entity.Property<DateTime>("CreatedDate").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property<string>("CreatedBy").IsRequired().HasMaxLength(100).HasDefaultValue("SYSTEM");
            }
        }

        // ----------------------- SaveChanges hooks -----------------------
        public override int SaveChanges()
        {
            SetSecuritySidecars();   // maintain hash sidecars for encrypted fields
            SetAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetSecuritySidecars();
            SetAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void SetAuditFields()
        {
            var nowUtc = DateTime.UtcNow;
            var user = Environment.UserName ?? "SYSTEM";

            foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
            {
                var createdDateProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedDate");
                if (createdDateProp is not null && createdDateProp.CurrentValue is null)
                    createdDateProp.CurrentValue = nowUtc;

                var createdByProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedBy");
                if (createdByProp is not null && (createdByProp.CurrentValue is null || string.IsNullOrWhiteSpace(createdByProp.CurrentValue.ToString())))
                    createdByProp.CurrentValue = user;
            }
        }

        // Maintain shadow hash columns for encrypted fields so we can index/search/uniquely constrain them.
        private void SetSecuritySidecars()
        {
            foreach (var entry in ChangeTracker.Entries<Customer>()
                         .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                var emailNorm = NormalizeEmail(entry.Entity.Email);
                var phoneNorm = NormalizePhone(entry.Entity.Phone);

                entry.Property("EmailHash").CurrentValue = HashOrNull(emailNorm);
                entry.Property("PhoneHash").CurrentValue = HashOrNull(phoneNorm);
            }

            foreach (var entry in ChangeTracker.Entries<Vehicle>()
                         .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                var plateNorm = NormalizeUpper(entry.Entity.LicensePlate);
                var vinNorm = NormalizeUpper(entry.Entity.VIN);

                entry.Property("LicensePlateHash").CurrentValue = HashOrNull(plateNorm);
                entry.Property("VinHash").CurrentValue = HashOrNull(vinNorm);
            }
        }

        // -------- Normalization & hashing helpers (shadow columns only) --------
        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return email.Trim().ToUpperInvariant(); // stable, case-insensitive match
        }

        private static string? NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return string.IsNullOrWhiteSpace(digits) ? null : digits;
        }

        private static string? NormalizeUpper(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim().ToUpperInvariant();
        }

        private static string? HashOrNull(string? normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized)) return null;
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString(); // 64 hex chars
        }
    }
}

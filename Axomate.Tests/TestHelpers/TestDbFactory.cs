// File: Axomate.Tests/TestHelpers/TestDbFactory.cs
using System;
using System.IO;
using Axomate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Axomate.Tests.TestHelpers
{
    public static class TestDbFactory
    {
        public static AxomateDbContext MakeDb()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"axomate_tests_{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<AxomateDbContext>()
                .UseSqlite($"Data Source={dbPath};Cache=Shared")
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options;

            var ctx = new AxomateDbContext(options);
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
            return ctx;
        }
    }
}

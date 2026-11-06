using System;
using System.Data.Entity.Migrations;
using System.Linq;
using ReadingsApp.Models;

namespace ReadingsApp.Data
{
    /// <summary>
    /// Code first automatic migrations: creates the Readings table on first use, even in an
    /// existing empty database such as Azure SQL, then seeds 5 sample readings once.
    /// </summary>
    public sealed class ReadingsMigrationsConfiguration : DbMigrationsConfiguration<ReadingsDbContext>
    {
        public ReadingsMigrationsConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "ReadingsApp";
        }

        protected override void Seed(ReadingsDbContext db)
        {
            if (db.Readings.Any()) return;

            var today = DateTime.Today;
            var now = DateTime.UtcNow;
            db.Readings.AddRange(new[]
            {
                new Reading { MeterId = "MTR-001", Value = 1200.5m, ReadingDate = today.AddDays(-60), CreatedUtc = now },
                new Reading { MeterId = "MTR-001", Value = 1350.0m, ReadingDate = today.AddDays(-30), CreatedUtc = now },
                new Reading { MeterId = "MTR-001", Value = 1490.2m, ReadingDate = today.AddDays(-1), CreatedUtc = now },
                new Reading { MeterId = "MTR-002", Value = 512.0m, ReadingDate = today.AddDays(-45), CreatedUtc = now },
                new Reading { MeterId = "MTR-002", Value = 640.7m, ReadingDate = today.AddDays(-15), CreatedUtc = now }
            });
            db.SaveChanges();
        }
    }
}

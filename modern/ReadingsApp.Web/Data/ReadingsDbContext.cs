using Microsoft.EntityFrameworkCore;
using ReadingsApp.Models;

namespace ReadingsApp.Web.Data;

/// <summary>EF Core mapping of the existing Readings table (same shape the old EF6 app created).</summary>
public class ReadingsDbContext(DbContextOptions<ReadingsDbContext> options) : DbContext(options)
{
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var reading = modelBuilder.Entity<Reading>();
        reading.ToTable("Readings");
        reading.Property(r => r.MeterId).IsRequired().HasMaxLength(Reading.MeterIdMaxLength);
        reading.Property(r => r.Value).HasPrecision(18, 3);
        reading.Property(r => r.ReadingDate).HasColumnType("date");
        reading.Property(r => r.CreatedUtc).HasColumnType("datetime2");
        reading.HasIndex(r => new { r.MeterId, r.ReadingDate }).HasDatabaseName("IX_Readings_MeterId_ReadingDate");
    }
}

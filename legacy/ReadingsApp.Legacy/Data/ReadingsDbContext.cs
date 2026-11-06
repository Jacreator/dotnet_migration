using System.Data.Entity;
using ReadingsApp.Models;

namespace ReadingsApp.Data
{
    public class ReadingsDbContext : DbContext
    {
        /// <summary>Uses the "ReadingsDb" connection string from Web.config (the one place for it).</summary>
        public ReadingsDbContext() : base("name=ReadingsDb")
        {
        }

        public DbSet<Reading> Readings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var reading = modelBuilder.Entity<Reading>();
            reading.ToTable("Readings");
            reading.Property(r => r.MeterId).IsRequired().HasMaxLength(Reading.MeterIdMaxLength);
            reading.Property(r => r.Value).HasPrecision(18, 3);
            reading.Property(r => r.ReadingDate).HasColumnType("date");
            reading.Property(r => r.CreatedUtc).HasColumnType("datetime2");
            reading.HasIndex(r => new { r.MeterId, r.ReadingDate }).HasName("IX_Readings_MeterId_ReadingDate");
        }
    }
}

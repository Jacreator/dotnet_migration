using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ReadingsApp.Models;

namespace ReadingsApp.Data
{
    /// <summary>EF6 data access for readings.</summary>
    public class ReadingRepository : IReadingRepository
    {
        private readonly ReadingsDbContext _db;

        public ReadingRepository(ReadingsDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public void Add(Reading reading)
        {
            _db.Readings.Add(reading);
            _db.SaveChanges();
        }

        public IReadOnlyList<Reading> Query(string meterId)
        {
            IQueryable<Reading> query = _db.Readings.AsNoTracking();
            if (!string.IsNullOrEmpty(meterId))
                query = query.Where(r => r.MeterId == meterId);

            return query
                .OrderByDescending(r => r.ReadingDate)
                .ThenByDescending(r => r.CreatedUtc)
                .ToList();
        }
    }
}

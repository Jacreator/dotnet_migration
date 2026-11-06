using Microsoft.EntityFrameworkCore;
using ReadingsApp.Data;
using ReadingsApp.Models;

namespace ReadingsApp.Web.Data;

/// <summary>EF Core data access for readings.</summary>
public class ReadingRepository(ReadingsDbContext db) : IReadingRepository
{
    public void Add(Reading reading)
    {
        db.Readings.Add(reading);
        db.SaveChanges();
    }

    public IReadOnlyList<Reading> Query(string meterId)
    {
        IQueryable<Reading> query = db.Readings.AsNoTracking();
        if (!string.IsNullOrEmpty(meterId))
            query = query.Where(r => r.MeterId == meterId);

        return query
            .OrderByDescending(r => r.ReadingDate)
            .ThenByDescending(r => r.CreatedUtc)
            .ToList();
    }
}

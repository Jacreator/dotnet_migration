using System.Collections.Generic;
using ReadingsApp.Models;

namespace ReadingsApp.Data
{
    public interface IReadingRepository
    {
        void Add(Reading reading);

        /// <summary>Readings newest first (by ReadingDate, then CreatedUtc). Empty meter ID means all meters.</summary>
        IReadOnlyList<Reading> Query(string meterId);
    }
}

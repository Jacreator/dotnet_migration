using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ReadingsApp.Models;

namespace ReadingsApp.Services
{
    public interface IReadingService
    {
        /// <summary>Validates and saves a reading. Returns the errors; an empty list means it was saved.</summary>
        IReadOnlyList<ValidationResult> Submit(ReadingInput input);

        /// <summary>Readings newest first. A blank meter ID returns every reading.</summary>
        IReadOnlyList<Reading> GetByMeter(string meterId);
    }
}

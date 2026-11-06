using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ReadingsApp.Data;
using ReadingsApp.Models;

namespace ReadingsApp.Services
{
    /// <summary>The one place for the reading rules.</summary>
    public class ReadingService : IReadingService
    {
        private readonly IReadingRepository _repository;

        public ReadingService(IReadingRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public IReadOnlyList<ValidationResult> Submit(ReadingInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var meterId = Normalise(input.MeterId);
            var errors = Validate(meterId, input);
            if (errors.Count > 0) return errors;

            _repository.Add(new Reading
            {
                MeterId = meterId,
                Value = input.Value.Value,
                ReadingDate = input.ReadingDate.Value.Date,
                CreatedUtc = DateTime.UtcNow
            });
            return errors;
        }

        public IReadOnlyList<Reading> GetByMeter(string meterId)
        {
            return _repository.Query(Normalise(meterId));
        }

        private List<ValidationResult> Validate(string meterId, ReadingInput input)
        {
            var errors = new List<ValidationResult>();

            if (meterId.Length == 0)
                errors.Add(Error("Meter ID is required.", nameof(input.MeterId)));
            else if (meterId.Length > Reading.MeterIdMaxLength)
                errors.Add(Error($"Meter ID must be {Reading.MeterIdMaxLength} characters or fewer.", nameof(input.MeterId)));

            if (input.Value == null)
                errors.Add(Error("Reading value is required.", nameof(input.Value)));
            else if (input.Value < 0)
                errors.Add(Error("Reading value must be zero or more.", nameof(input.Value)));

            if (input.ReadingDate == null)
                errors.Add(Error("Date is required.", nameof(input.ReadingDate)));
            else if (input.ReadingDate.Value.Date > DateTime.Today)
                errors.Add(Error("Date must not be in the future.", nameof(input.ReadingDate)));

            // Only check against history once the basic fields are valid.
            if (errors.Count == 0)
            {
                var previous = _repository.Query(meterId).FirstOrDefault();
                if (previous != null && input.Value < previous.Value)
                    errors.Add(Error(
                        $"Reading {input.Value} is lower than the previous reading for meter {meterId} " +
                        $"({previous.Value} on {previous.ReadingDate:dd MMM yyyy}).",
                        nameof(input.Value)));
            }

            return errors;
        }

        private static string Normalise(string meterId)
        {
            return (meterId ?? string.Empty).Trim();
        }

        private static ValidationResult Error(string message, string field)
        {
            return new ValidationResult(message, new[] { field });
        }
    }
}

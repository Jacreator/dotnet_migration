using System;
using System.Collections.Generic;
using System.Linq;
using ReadingsApp.Data;
using ReadingsApp.Models;
using ReadingsApp.Services;
using Xunit;

namespace ReadingsApp.Tests;

public class ReadingServiceTests
{
    private readonly FakeRepository _repository = new();
    private readonly ReadingService _service;

    public ReadingServiceTests()
    {
        _repository.Add(new Reading { MeterId = "M1", Value = 100, ReadingDate = DateTime.Today.AddDays(-5) });
        _service = new ReadingService(_repository);
    }

    [Fact]
    public void Rejects_empty_meter_id() =>
        AssertRejected(new ReadingInput { MeterId = " ", Value = 1, ReadingDate = DateTime.Today }, nameof(ReadingInput.MeterId));

    [Fact]
    public void Rejects_negative_value() =>
        AssertRejected(new ReadingInput { MeterId = "M2", Value = -1, ReadingDate = DateTime.Today }, nameof(ReadingInput.Value));

    [Fact]
    public void Rejects_future_date() =>
        AssertRejected(new ReadingInput { MeterId = "M2", Value = 1, ReadingDate = DateTime.Today.AddDays(1) }, nameof(ReadingInput.ReadingDate));

    [Fact]
    public void Rejects_value_lower_than_previous_reading() =>
        AssertRejected(new ReadingInput { MeterId = "M1", Value = 99, ReadingDate = DateTime.Today }, nameof(ReadingInput.Value));

    [Fact]
    public void Accepts_valid_reading()
    {
        var errors = _service.Submit(new ReadingInput { MeterId = " M1 ", Value = 150, ReadingDate = DateTime.Today });

        Assert.Empty(errors);
        Assert.Equal("M1", _repository.Query("M1").First().MeterId);
        Assert.Equal(150, _repository.Query("M1").First().Value);
    }

    private void AssertRejected(ReadingInput input, string field)
    {
        var errors = _service.Submit(input);

        Assert.Contains(errors, e => e.MemberNames.Contains(field));
        Assert.Single(_repository.Query(""));
    }

    private sealed class FakeRepository : IReadingRepository
    {
        private readonly List<Reading> _items = new();

        public void Add(Reading reading) => _items.Add(reading);

        public IReadOnlyList<Reading> Query(string meterId) => _items
            .Where(r => meterId.Length == 0 || r.MeterId == meterId)
            .OrderByDescending(r => r.ReadingDate)
            .ToList();
    }
}

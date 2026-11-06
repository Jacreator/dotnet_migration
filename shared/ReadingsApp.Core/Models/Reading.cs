using System;

namespace ReadingsApp.Models
{
    /// <summary>A saved meter reading. Maps to the Readings table.</summary>
    public class Reading
    {
        /// <summary>Shared by the service rule and the database column so the number lives in one place.</summary>
        public const int MeterIdMaxLength = 20;

        public int Id { get; set; }
        public string MeterId { get; set; }
        public decimal Value { get; set; }
        public DateTime ReadingDate { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}

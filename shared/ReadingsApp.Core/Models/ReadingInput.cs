using System;
using System.ComponentModel.DataAnnotations;

namespace ReadingsApp.Models
{
    /// <summary>
    /// What the Create form posts. Fields are nullable so a missing value reaches the service,
    /// which holds every validation rule. Attributes here are only for labels.
    /// </summary>
    public class ReadingInput
    {
        [Display(Name = "Meter ID")]
        public string MeterId { get; set; }

        [Display(Name = "Reading value")]
        public decimal? Value { get; set; }

        [Display(Name = "Date")]
        public DateTime? ReadingDate { get; set; }
    }
}

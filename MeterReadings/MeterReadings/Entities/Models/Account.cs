﻿namespace MeterReadings.Entities.Models
{
    public class Account
    {
        public int AccountId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
    }
}

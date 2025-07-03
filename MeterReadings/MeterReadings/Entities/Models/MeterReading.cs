namespace MeterReadings.Entities.Models
{
    public class MeterReading
    {
        public int MeterReadingId { get; set; }
        public int AccountId { get; set; }
        public DateTime MeterReadingDateTime { get; set; }
        public int MeterReadValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Account Account { get; set; } = null!;
    }
}

using MeterReadings.DataBase;
using MeterReadings.Entities.DTOs;
using MeterReadings.Entities.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace MeterReadings.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MeterReadingUploadsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MeterReadingUploadsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Route("/meter-reading-uploads")]
        public async Task<IActionResult> UploadMeterReadings(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            int successCount = 0;
            int failureCount = 0;

            var existingAccountIds = _context.Accounts.Select(a => a.AccountId).ToHashSet();
            var existingReadings = _context.Set<MeterReading>()
                .Select(m => new { m.AccountId, m.MeterReadingDateTime })
                .ToHashSet();

            var newReadings = new List<MeterReading>();

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                string? line;
                bool isFirstLine = true;
                while ((line = await stream.ReadLineAsync()) != null)
                {
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        continue; // skip header
                    }
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length < 3)
                    {
                        failureCount++;
                        continue;
                    }

                    // Parse AccountId
                    if (!int.TryParse(parts[0], out int accountId) || !existingAccountIds.Contains(accountId))
                    {
                        failureCount++;
                        continue;
                    }

                    // Parse DateTime with invariant culture and exact format
                    if (!DateTime.TryParseExact(
                            parts[1].Trim(),
                            "dd/MM/yyyy HH:mm",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime readingDateTime))
                    {
                        failureCount++;
                        continue;
                    }

                    // Validate MeterReadValue (must be 5 digits)
                    var valueStr = parts[2].Trim();
                    if (!System.Text.RegularExpressions.Regex.IsMatch(valueStr, @"^\d{5}$"))
                    {
                        failureCount++;
                        continue;
                    }
                    int meterReadValue = int.Parse(valueStr);

                    // Check for duplicate in DB or in this batch
                    var readingKey = new { AccountId = accountId, MeterReadingDateTime = readingDateTime };
                    if (existingReadings.Contains(readingKey) ||
                        newReadings.Any(r => r.AccountId == accountId && r.MeterReadingDateTime == readingDateTime))
                    {
                        failureCount++;
                        continue;
                    }

                    // Ensure the new read isn’t older than the latest existing read for the account
                    var latestExistingRead = _context.MeterReadings
                        .Where(r => r.AccountId == accountId)
                        .OrderByDescending(r => r.MeterReadingDateTime)
                        .FirstOrDefault();

                    if (latestExistingRead != null && readingDateTime < latestExistingRead.MeterReadingDateTime)
                    {
                        failureCount++;
                        continue;
                    }
                    if (existingReadings.Contains(readingKey) ||
                        newReadings.Any(r => r.AccountId == accountId && r.MeterReadingDateTime == readingDateTime))
                    {
                        failureCount++;
                        continue;
                    }

                    // All validations passed
                    newReadings.Add(new MeterReading
                    {
                        AccountId = accountId,
                        MeterReadingDateTime = readingDateTime,
                        MeterReadValue = meterReadValue,
                        CreatedAt = DateTime.UtcNow
                    });
                    successCount++;
                }
            }

            if (newReadings.Count > 0)
            {
                _context.AddRange(newReadings);
                await _context.SaveChangesAsync();
            }

            return Ok(new MeterReadingUploadResultDto { Success = successCount, Failed = failureCount });
        }
    }
}

using MeterReadings.Controllers;
using MeterReadings.DataBase;
using MeterReadings.Entities.DTOs;
using MeterReadings.Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace MeterReadings.Tests.Controllers
{
    [TestFixture]
    public class MeterReadingUploadsControllerTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            context.Accounts.Add(new Account { AccountId = 1234, FirstName = "Test", LastName = "User" });
            context.SaveChanges();
            return context;
        }

        private IFormFile CreateTestFile(string content)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            return new FormFile(stream, 0, stream.Length, "file", "test.csv");
        }

        [Test]
        public async Task UploadMeterReadings_ValidFile_ReturnsSuccess()
        {
            var context = GetDbContext();
            var controller = new MeterReadingUploadsController(context);
            var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n1234,22/04/2019 09:24,12345";
            var file = CreateTestFile(csv);

            var result = await controller.UploadMeterReadings(file);

            var okResult = result as OkObjectResult;
            MeterReadingUploadResultDto? resultDto = okResult?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(okResult, Is.Not.Null, "Result should be OkObjectResult");
                Assert.That(resultDto, Is.Not.Null, "Result DTO should not be null");
                Assert.That(resultDto?.Success, Is.EqualTo(1), "Success count should be 1");
                Assert.That(resultDto?.Failed, Is.EqualTo(0), "Failed count should be 0");
                Assert.That(context.MeterReadings.Count(), Is.EqualTo(1), "Should have 1 meter reading in DB");
            });
        }

        [Test]
        public async Task UploadMeterReadings_InvalidAccountId_Fails()
        {
            var context = GetDbContext();
            var controller = new MeterReadingUploadsController(context);
            var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n9999,22/04/2019 09:24,12345";
            var file = CreateTestFile(csv);

            var result = await controller.UploadMeterReadings(file);

            var okResult = result as OkObjectResult;
            var resultDto = okResult?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(resultDto, Is.Not.Null);
                Assert.That(resultDto?.Success, Is.EqualTo(0));
                Assert.That(resultDto?.Failed, Is.EqualTo(1));
                Assert.That(context.MeterReadings.Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task UploadMeterReadings_InvalidDate_Fails()
        {
            var context = GetDbContext();
            var controller = new MeterReadingUploadsController(context);
            var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n1234,not-a-date,12345";
            var file = CreateTestFile(csv);

            var result = await controller.UploadMeterReadings(file);

            var okResult = result as OkObjectResult;
            var resultDto = okResult?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(resultDto, Is.Not.Null);
                Assert.That(resultDto?.Success, Is.EqualTo(0));
                Assert.That(resultDto?.Failed, Is.EqualTo(1));
                Assert.That(context.MeterReadings.Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task UploadMeterReadings_InvalidMeterValue_Fails()
        {
            var context = GetDbContext();
            var controller = new MeterReadingUploadsController(context);
            var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n1234,22/04/2019 09:24,12";
            var file = CreateTestFile(csv);

            var result = await controller.UploadMeterReadings(file);

            var okResult = result as OkObjectResult;
            var resultDto = okResult?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(resultDto, Is.Not.Null);
                Assert.That(resultDto?.Success, Is.EqualTo(0));
                Assert.That(resultDto?.Failed, Is.EqualTo(1));
                Assert.That(context.MeterReadings.Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task UploadMeterReadings_DuplicateInFile_OneSuccessOneFail()
        {
            var context = GetDbContext();
            var controller = new MeterReadingUploadsController(context);
            var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n1234,22/04/2019 09:24,12345\n1234,22/04/2019 09:24,12345";
            var file = CreateTestFile(csv);

            var result = await controller.UploadMeterReadings(file);

            var okResult = result as OkObjectResult;
            var resultDto = okResult?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(resultDto, Is.Not.Null);
                Assert.That(resultDto?.Success, Is.EqualTo(1));
                Assert.That(resultDto?.Failed, Is.EqualTo(1));
                Assert.That(context.MeterReadings.Count(), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task UploadMeterReadings_DuplicateInDatabase_Fails()
        {
            var context = GetDbContext();
            context.MeterReadings.Add(new MeterReading
            {
                AccountId = 1234,
                MeterReadingDateTime = new DateTime(2019, 4, 22, 9, 24, 0),
                MeterReadValue = 12345,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
            var controller = new MeterReadingUploadsController(context);
            var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n1234,22/04/2019 09:24,12345";
            var file = CreateTestFile(csv);

            var result = await controller.UploadMeterReadings(file);

            var okResult = result as OkObjectResult;
            var resultDto = okResult?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(resultDto, Is.Not.Null);
                Assert.That(resultDto?.Success, Is.EqualTo(0));
                Assert.That(resultDto?.Failed, Is.EqualTo(1));
                Assert.That(context.MeterReadings.Count(), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task UploadMeterReadings_EmptyFile_ReturnsBadRequest()
        {
            var context = GetDbContext();
            var controller = new MeterReadingUploadsController(context);
            var file = CreateTestFile(string.Empty);

            var result = await controller.UploadMeterReadings(file);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UploadMeterReadings_OlderThanLatestExistingRead_Fails()
        {
            var context = GetDbContext();
            context.MeterReadings.Add(new MeterReading
            {
                AccountId = 1234,
                MeterReadingDateTime = new DateTime(2020, 1, 1, 10, 0, 0),
                MeterReadValue = 54321,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
            var controller = new MeterReadingUploadsController(context);
            var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n1234,01/01/2019 09:24,12345";
            var file = CreateTestFile(csv);

            var result = await controller.UploadMeterReadings(file);

            var okResult = result as OkObjectResult;
            var resultDto = okResult?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(resultDto, Is.Not.Null);
                Assert.That(resultDto?.Success, Is.EqualTo(0));
                Assert.That(resultDto?.Failed, Is.EqualTo(1));
                Assert.That(context.MeterReadings.Count(), Is.EqualTo(1), "Should still have only the original reading");
            });
        }

        [Test]
        public async Task UploadMeterReadings_NewerThanLatestExistingRead_Succeeds()
        {
            var context = GetDbContext();
            context.MeterReadings.Add(new MeterReading
            {
                AccountId = 1234,
                MeterReadingDateTime = new DateTime(2019, 1, 1, 9, 24, 0),
                MeterReadValue = 11111,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
            var controller = new MeterReadingUploadsController(context);
            var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n1234,01/01/2020 10:00,22222";
            var file = CreateTestFile(csv);

            var result = await controller.UploadMeterReadings(file);

            var okResult = result as OkObjectResult;
            var resultDto = okResult?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(resultDto, Is.Not.Null);
                Assert.That(resultDto?.Success, Is.EqualTo(1));
                Assert.That(resultDto?.Failed, Is.EqualTo(0));
                Assert.That(context.MeterReadings.Count(), Is.EqualTo(2), "Should have both readings");
            });
        }

        [Test]
        public async Task UploadMeterReadings_MeterReading1Csv_CorrectSuccessAndFailedCounts()
        {
            var context = GetDbContext();
            var controller = new MeterReadingUploadsController(context);
            var validAccountIds = new[]// Add all valid account IDs from Test_Accounts 1.csv
            {
                1234,1239,1240,1241,1242,1243,1244,1245,1246,1247,1248,
                2233,2344,2345,2346,2347,2348,2349,2350,2351,2352,2353,
                2355,2356,4534,6776,8766
            };
            foreach (var id in validAccountIds)
            {
                if (!context.Accounts.Any(a => a.AccountId == id))
                    context.Accounts.Add(new Account { AccountId = id });
            }
            context.SaveChanges();
            var csv = File.ReadAllText("Resources/Meter_Reading 1.csv");
            var file = CreateTestFile(csv);

            var result = await controller.UploadMeterReadings(file);

            var okResult = result as OkObjectResult;
            var resultDto = okResult?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(resultDto, Is.Not.Null);
                Assert.That(resultDto?.Success, Is.EqualTo(4), "Success count should be 4");
                Assert.That(resultDto?.Failed, Is.EqualTo(31), "Failed count should be 31");
            });
        }

        [Test]
        public async Task UploadMeterReadings_MeterReading1Csv_Twice_SecondAllFailed()
        {
            var context = GetDbContext();
            var controller = new MeterReadingUploadsController(context);
            var validAccountIds = new[]// Add all valid account IDs from Test_Accounts 1.csv
            {
                1234,1239,1240,1241,1242,1243,1244,1245,1246,1247,1248,
                2233,2344,2345,2346,2347,2348,2349,2350,2351,2352,2353,
                2355,2356,4534,6776,8766
            };
            foreach (var id in validAccountIds)
            {
                if (!context.Accounts.Any(a => a.AccountId == id))
                    context.Accounts.Add(new Account { AccountId = id });
            }
            context.SaveChanges();
            var csv = File.ReadAllText("Resources/Meter_Reading 1.csv");
            var file1 = CreateTestFile(csv);
            var file2 = CreateTestFile(csv);

            // First upload
            await controller.UploadMeterReadings(file1);
            // Second upload
            var result2 = await controller.UploadMeterReadings(file2);

            var okResult2 = result2 as OkObjectResult;
            var resultDto2 = okResult2?.Value as MeterReadingUploadResultDto;
            Assert.Multiple(() =>
            {
                Assert.That(resultDto2, Is.Not.Null);
                Assert.That(resultDto2?.Success, Is.EqualTo(0), "Second upload: Success count should be 0");
                Assert.That(resultDto2?.Failed, Is.EqualTo(35), "Second upload: Failed count should be 35");
            });
        }
    }
}

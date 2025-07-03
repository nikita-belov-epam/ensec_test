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
    }
}

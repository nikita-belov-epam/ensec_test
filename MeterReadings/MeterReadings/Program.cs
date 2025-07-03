using MeterReadings.DataBase;
using MeterReadings.Entities.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the ApplicationDbContext with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=meterreadings.db"));


var app = builder.Build();

// Seed the database with initial data
using (var scope = app.Services.CreateScope())
{
    // Delete the SQLite database file to start fresh each run
    var dbPath = "meterreadings.db";
    if (File.Exists(dbPath))
    {
        File.Delete(dbPath);
    }

    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    if (!db.Accounts.Any())
    {
        var lines = File.ReadAllLines("Resources/Test_Accounts 1.csv")
                        .Skip(1); // skip header

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (int.TryParse(parts[0], out int accountId))
            {
                db.Accounts.Add(new Account
                {
                    AccountId = accountId,
                    FirstName = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1],
                    LastName = string.IsNullOrWhiteSpace(parts[2]) ? null : parts[2]
                });
            }
        }
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

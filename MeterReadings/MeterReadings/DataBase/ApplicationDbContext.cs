using MeterReadings.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace MeterReadings.DataBase
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<MeterReading> MeterReadings { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
                .HasKey(a => a.AccountId);

            modelBuilder.Entity<MeterReading>()
                .HasKey(m => m.MeterReadingId);

            modelBuilder.Entity<MeterReading>()
                .HasOne(m => m.Account)
                .WithMany(a => a.MeterReadings)
                .HasForeignKey(m => m.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MeterReading>()
                .HasIndex(m => new { m.AccountId, m.MeterReadingDateTime })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}

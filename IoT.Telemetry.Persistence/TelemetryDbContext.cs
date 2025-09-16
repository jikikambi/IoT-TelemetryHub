using IoT.Telemetry.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoT.Telemetry.Persistence;

public class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : DbContext(options)
{
    public DbSet<TelemetryLog> TelemetryLogs => Set<TelemetryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryLog>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<TelemetryLog>()
            .Property(t => t.Timestamp)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
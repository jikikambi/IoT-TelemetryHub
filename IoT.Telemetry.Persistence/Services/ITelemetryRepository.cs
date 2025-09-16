using IoT.Telemetry.Persistence.Entities;

namespace IoT.Telemetry.Persistence.Services;

public interface ITelemetryRepository
{
    Task AddAsync(TelemetryLog log, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TelemetryLog> logs, CancellationToken ct = default);
    Task FlushAsync(CancellationToken ct = default);
}
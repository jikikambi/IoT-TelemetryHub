using IoT.Telemetry.Persistence.Entities;

namespace IoT.Telemetry.Persistence.Services;

public class TelemetryRepository : ITelemetryRepository, IDisposable, IAsyncDisposable
{
    private readonly TelemetryDbContext _ctx;
    private readonly List<TelemetryLog> _buffer = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly int _maxBatchSize;
    private readonly TimeSpan _maxFlushInterval;
    private DateTime _lastFlushTime;

    public TelemetryRepository(
        TelemetryDbContext db,
        int maxBatchSize = 50,
        TimeSpan? maxFlushInterval = null)
    {
        _ctx = db;
        _maxBatchSize = maxBatchSize;
        _maxFlushInterval = maxFlushInterval ?? TimeSpan.FromSeconds(5);
        _lastFlushTime = DateTime.UtcNow;

        // Start background flush loop
        _ = Task.Run(PeriodicFlushLoopAsync);
    }

    public async Task AddAsync(TelemetryLog log, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _buffer.Add(log);
            if (_buffer.Count >= _maxBatchSize)
                await FlushInternalAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AddRangeAsync(IEnumerable<TelemetryLog> logs, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _buffer.AddRange(logs);
            if (_buffer.Count >= _maxBatchSize)
                await FlushInternalAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task FlushAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await FlushInternalAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task FlushInternalAsync(CancellationToken ct = default)
    {
        if (_buffer.Count == 0) return;

        await _ctx.TelemetryLogs.AddRangeAsync(_buffer, ct);
        await _ctx.SaveChangesAsync(ct);
        _buffer.Clear();
        _lastFlushTime = DateTime.UtcNow;
    }

    private async Task PeriodicFlushLoopAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(_maxFlushInterval);

                await _lock.WaitAsync();
                try
                {
                    // Flush if buffer has data and max interval elapsed
                    if (_buffer.Count > 0 &&
                        DateTime.UtcNow - _lastFlushTime >= _maxFlushInterval)
                    {
                        await FlushInternalAsync();
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception ex)
            {
                // Log error if you have a logger, otherwise ignore
                Console.WriteLine($"Periodic flush error: {ex}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await FlushAsync();
        _lock.Dispose();
    }

    public void Dispose()
    {
        // Fall back to sync dispose
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

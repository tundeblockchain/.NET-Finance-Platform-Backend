namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Tracks whether this worker process is considered healthy for lease heartbeats.
/// </summary>
public sealed class WorkerHealthTracker
{
    private readonly object _gate = new();
    private bool _isHealthy = true;
    private string? _unhealthyReason;

    public bool IsHealthy
    {
        get
        {
            lock (_gate)
            {
                return _isHealthy;
            }
        }
    }

    public string? UnhealthyReason
    {
        get
        {
            lock (_gate)
            {
                return _unhealthyReason;
            }
        }
    }

    public void MarkHealthy()
    {
        lock (_gate)
        {
            _isHealthy = true;
            _unhealthyReason = null;
        }
    }

    public void MarkUnhealthy(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        lock (_gate)
        {
            _isHealthy = false;
            _unhealthyReason = reason;
        }
    }
}

using NCcsds.Sle.Common;
using NCcsds.Sle.Raf;

namespace NCcsds.Sle.Cltu;

/// <summary>
/// CLTU (Command Link Transmission Unit) service instance.
/// </summary>
public class CltuServiceInstance
{
    private SleServiceState _state = SleServiceState.Unbound;
    private readonly object _lock = new();
    private long _cltuIdCounter;

    /// <summary>
    /// Service instance identifier.
    /// </summary>
    public string ServiceInstanceId { get; set; } = string.Empty;

    /// <summary>
    /// Current service state.
    /// </summary>
    public SleServiceState State
    {
        get { lock (_lock) return _state; }
        private set { lock (_lock) _state = value; }
    }

    /// <summary>
    /// SLE version negotiated.
    /// </summary>
    public SleVersion Version { get; set; } = SleVersion.V5;

    /// <summary>
    /// Credentials for authentication.
    /// </summary>
    public SleCredentials? Credentials { get; set; }

    /// <summary>
    /// Statistics for this service instance.
    /// </summary>
    public CltuStatistics Statistics { get; } = new();

    /// <summary>
    /// Event raised when the state changes.
    /// </summary>
    public event Action<SleServiceState>? StateChanged;

    /// <summary>
    /// Event raised when CLTU radiation is complete.
    /// </summary>
    public event Action<CltuRadiationNotification>? CltuRadiated;

    /// <summary>
    /// Event raised when production status changes.
    /// </summary>
    public event Action<CltuProductionStatus>? ProductionStatusChanged;

    /// <summary>
    /// Initiates a BIND operation.
    /// </summary>
    public async Task<SleBindResult> BindAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_state != SleServiceState.Unbound)
                return new SleBindResult(false, SleBindDiagnostic.AlreadyBound);

            _state = SleServiceState.BindPending;
        }

        StateChanged?.Invoke(SleServiceState.BindPending);

        await Task.Delay(100, cancellationToken);

        lock (_lock)
        {
            _state = SleServiceState.Ready;
        }

        StateChanged?.Invoke(SleServiceState.Ready);
        return new SleBindResult(true);
    }

    /// <summary>
    /// Initiates a START operation.
    /// </summary>
    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_state != SleServiceState.Ready)
                return false;

            _state = SleServiceState.StartPending;
        }

        StateChanged?.Invoke(SleServiceState.StartPending);

        await Task.Delay(100, cancellationToken);

        lock (_lock)
        {
            _state = SleServiceState.Active;
        }

        StateChanged?.Invoke(SleServiceState.Active);
        return true;
    }

    /// <summary>
    /// Transfers a CLTU for radiation.
    /// </summary>
    /// <param name="cltuData">The CLTU data.</param>
    /// <param name="earliestRadiationTime">Earliest time to radiate (optional).</param>
    /// <param name="latestRadiationTime">Latest time to radiate (optional).</param>
    /// <param name="delayTime">Delay between CLTUs (optional).</param>
    /// <returns>The CLTU transfer result.</returns>
    public async Task<CltuTransferResult> TransferCltuAsync(
        byte[] cltuData,
        DateTime? earliestRadiationTime = null,
        DateTime? latestRadiationTime = null,
        TimeSpan? delayTime = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_state != SleServiceState.Active)
                return new CltuTransferResult(false, -1, "Service not active");
        }

        long cltuId = Interlocked.Increment(ref _cltuIdCounter);

        Statistics.CltusReceived++;

        // In a real implementation, this would send the CLTU-TRANSFER-DATA PDU
        await Task.Delay(50, cancellationToken);

        Statistics.CltusQueued++;

        return new CltuTransferResult(true, cltuId);
    }

    /// <summary>
    /// Gets the current production status.
    /// </summary>
    public async Task<CltuProductionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);

        return new CltuProductionStatus
        {
            CltusReceived = Statistics.CltusReceived,
            CltusProcessed = Statistics.CltusProcessed,
            CltusRadiated = Statistics.CltusRadiated,
            ProductionStatus = CltuProductionState.Operational
        };
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_state != SleServiceState.Active)
                return false;

            _state = SleServiceState.StopPending;
        }

        StateChanged?.Invoke(SleServiceState.StopPending);

        await Task.Delay(100, cancellationToken);

        lock (_lock)
        {
            _state = SleServiceState.Ready;
        }

        StateChanged?.Invoke(SleServiceState.Ready);
        return true;
    }

    /// <summary>
    /// Unbinds the service.
    /// </summary>
    public async Task<bool> UnbindAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_state != SleServiceState.Ready)
                return false;

            _state = SleServiceState.UnbindPending;
        }

        StateChanged?.Invoke(SleServiceState.UnbindPending);

        await Task.Delay(100, cancellationToken);

        lock (_lock)
        {
            _state = SleServiceState.Unbound;
        }

        StateChanged?.Invoke(SleServiceState.Unbound);
        return true;
    }
}

/// <summary>
/// CLTU transfer result.
/// </summary>
public readonly struct CltuTransferResult
{
    /// <summary>
    /// Whether the transfer was accepted.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Assigned CLTU ID.
    /// </summary>
    public long CltuId { get; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; }

    public CltuTransferResult(bool success, long cltuId, string? errorMessage = null)
    {
        Success = success;
        CltuId = cltuId;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// CLTU radiation notification.
/// </summary>
public class CltuRadiationNotification
{
    /// <summary>
    /// CLTU ID.
    /// </summary>
    public long CltuId { get; set; }

    /// <summary>
    /// Radiation start time.
    /// </summary>
    public DateTime RadiationStartTime { get; set; }

    /// <summary>
    /// Radiation stop time.
    /// </summary>
    public DateTime RadiationStopTime { get; set; }

    /// <summary>
    /// Radiation status.
    /// </summary>
    public CltuRadiationStatus Status { get; set; }
}

/// <summary>
/// CLTU radiation status.
/// </summary>
public enum CltuRadiationStatus
{
    /// <summary>
    /// Successfully radiated.
    /// </summary>
    Radiated,

    /// <summary>
    /// Expired before radiation.
    /// </summary>
    Expired,

    /// <summary>
    /// Interrupted.
    /// </summary>
    Interrupted,

    /// <summary>
    /// Radiation failed.
    /// </summary>
    Failed
}

/// <summary>
/// CLTU production status.
/// </summary>
public class CltuProductionStatus
{
    /// <summary>
    /// Number of CLTUs received.
    /// </summary>
    public long CltusReceived { get; set; }

    /// <summary>
    /// Number of CLTUs processed.
    /// </summary>
    public long CltusProcessed { get; set; }

    /// <summary>
    /// Number of CLTUs radiated.
    /// </summary>
    public long CltusRadiated { get; set; }

    /// <summary>
    /// Current production state.
    /// </summary>
    public CltuProductionState ProductionStatus { get; set; }
}

/// <summary>
/// CLTU production state.
/// </summary>
public enum CltuProductionState
{
    /// <summary>
    /// Operational.
    /// </summary>
    Operational,

    /// <summary>
    /// Configured.
    /// </summary>
    Configured,

    /// <summary>
    /// Interrupted.
    /// </summary>
    Interrupted,

    /// <summary>
    /// Halted.
    /// </summary>
    Halted
}

/// <summary>
/// CLTU service statistics.
/// </summary>
public class CltuStatistics
{
    /// <summary>
    /// Number of CLTUs received.
    /// </summary>
    public long CltusReceived { get; set; }

    /// <summary>
    /// Number of CLTUs queued.
    /// </summary>
    public long CltusQueued { get; set; }

    /// <summary>
    /// Number of CLTUs processed.
    /// </summary>
    public long CltusProcessed { get; set; }

    /// <summary>
    /// Number of CLTUs radiated.
    /// </summary>
    public long CltusRadiated { get; set; }
}

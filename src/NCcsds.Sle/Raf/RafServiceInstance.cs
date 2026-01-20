using NCcsds.Sle.Common;

namespace NCcsds.Sle.Raf;

/// <summary>
/// RAF (Return All Frames) service instance.
/// </summary>
public class RafServiceInstance
{
    private SleServiceState _state = SleServiceState.Unbound;
    private readonly object _lock = new();

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
    /// Peer identifier (responder).
    /// </summary>
    public string PeerId { get; set; } = string.Empty;

    /// <summary>
    /// SLE version negotiated.
    /// </summary>
    public SleVersion Version { get; set; } = SleVersion.V5;

    /// <summary>
    /// Frame quality filter.
    /// </summary>
    public RafFrameQuality RequestedFrameQuality { get; set; } = RafFrameQuality.AllFrames;

    /// <summary>
    /// Credentials for authentication.
    /// </summary>
    public SleCredentials? Credentials { get; set; }

    /// <summary>
    /// Statistics for this service instance.
    /// </summary>
    public RafStatistics Statistics { get; } = new();

    /// <summary>
    /// Event raised when the state changes.
    /// </summary>
    public event Action<SleServiceState>? StateChanged;

    /// <summary>
    /// Event raised when a frame is received.
    /// </summary>
    public event Action<RafTransferData>? FrameReceived;

    /// <summary>
    /// Event raised when a sync notification is received.
    /// </summary>
    public event Action<RafSyncNotification>? SyncNotificationReceived;

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

        // In a real implementation, this would send the BIND PDU
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
    public async Task<bool> StartAsync(DateTime? startTime = null, DateTime? stopTime = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_state != SleServiceState.Ready)
                return false;

            _state = SleServiceState.StartPending;
        }

        StateChanged?.Invoke(SleServiceState.StartPending);

        // In a real implementation, this would send the START PDU
        await Task.Delay(100, cancellationToken);

        lock (_lock)
        {
            _state = SleServiceState.Active;
        }

        StateChanged?.Invoke(SleServiceState.Active);
        return true;
    }

    /// <summary>
    /// Initiates a STOP operation.
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
    /// Initiates an UNBIND operation.
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

    /// <summary>
    /// Processes a received frame (called by transport layer).
    /// </summary>
    internal void ProcessFrame(RafTransferData frame)
    {
        Statistics.FramesReceived++;

        if (RequestedFrameQuality == RafFrameQuality.GoodFramesOnly && !frame.IsGoodFrame)
        {
            Statistics.FramesDiscarded++;
            return;
        }

        FrameReceived?.Invoke(frame);
    }
}

/// <summary>
/// RAF frame quality filter options.
/// </summary>
public enum RafFrameQuality
{
    /// <summary>
    /// Deliver all frames.
    /// </summary>
    AllFrames,

    /// <summary>
    /// Deliver only frames with valid CRC.
    /// </summary>
    GoodFramesOnly,

    /// <summary>
    /// Deliver only frames with errors.
    /// </summary>
    BadFramesOnly
}

/// <summary>
/// RAF transfer data (frame delivery).
/// </summary>
public class RafTransferData
{
    /// <summary>
    /// The frame data.
    /// </summary>
    public byte[] FrameData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Earth receive time.
    /// </summary>
    public DateTime EarthReceiveTime { get; set; }

    /// <summary>
    /// Antenna ID.
    /// </summary>
    public string AntennaId { get; set; } = string.Empty;

    /// <summary>
    /// Data link continuity indicator.
    /// </summary>
    public int DataLinkContinuity { get; set; }

    /// <summary>
    /// Frame quality.
    /// </summary>
    public bool IsGoodFrame { get; set; } = true;
}

/// <summary>
/// RAF sync notification.
/// </summary>
public class RafSyncNotification
{
    /// <summary>
    /// Notification type.
    /// </summary>
    public RafNotificationType Type { get; set; }

    /// <summary>
    /// Production status.
    /// </summary>
    public RafProductionStatus? ProductionStatus { get; set; }
}

/// <summary>
/// RAF notification types.
/// </summary>
public enum RafNotificationType
{
    /// <summary>
    /// Loss of frame sync.
    /// </summary>
    LossOfFrameSync,

    /// <summary>
    /// Production status change.
    /// </summary>
    ProductionStatusChange,

    /// <summary>
    /// End of data.
    /// </summary>
    EndOfData
}

/// <summary>
/// RAF production status.
/// </summary>
public enum RafProductionStatus
{
    /// <summary>
    /// Running.
    /// </summary>
    Running,

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
/// RAF service statistics.
/// </summary>
public class RafStatistics
{
    /// <summary>
    /// Number of frames received.
    /// </summary>
    public long FramesReceived { get; set; }

    /// <summary>
    /// Number of frames delivered.
    /// </summary>
    public long FramesDelivered { get; set; }

    /// <summary>
    /// Number of frames discarded.
    /// </summary>
    public long FramesDiscarded { get; set; }
}

/// <summary>
/// Result of a BIND operation.
/// </summary>
public readonly struct SleBindResult
{
    /// <summary>
    /// Whether the bind was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Diagnostic if bind failed.
    /// </summary>
    public SleBindDiagnostic? Diagnostic { get; }

    /// <summary>
    /// Provider version.
    /// </summary>
    public SleVersion? ProviderVersion { get; }

    public SleBindResult(bool success, SleBindDiagnostic? diagnostic = null, SleVersion? providerVersion = null)
    {
        Success = success;
        Diagnostic = diagnostic;
        ProviderVersion = providerVersion;
    }
}

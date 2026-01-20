using NCcsds.Sle.Common;
using NCcsds.Sle.Raf;

namespace NCcsds.Sle.Rocf;

/// <summary>
/// ROCF (Return Operational Control Field) service instance.
/// </summary>
public class RocfServiceInstance
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
    /// Requested global VCID.
    /// </summary>
    public RocfGlobalVcid? RequestedGlobalVcid { get; set; }

    /// <summary>
    /// Requested control word type filter.
    /// </summary>
    public RocfControlWordType RequestedControlWordType { get; set; } = RocfControlWordType.All;

    /// <summary>
    /// Requested TC virtual channel ID for filtering (-1 = all).
    /// </summary>
    public int RequestedTcVcid { get; set; } = -1;

    /// <summary>
    /// Credentials for authentication.
    /// </summary>
    public SleCredentials? Credentials { get; set; }

    /// <summary>
    /// Statistics for this service instance.
    /// </summary>
    public RocfStatistics Statistics { get; } = new();

    /// <summary>
    /// Event raised when the state changes.
    /// </summary>
    public event Action<SleServiceState>? StateChanged;

    /// <summary>
    /// Event raised when an OCF is received.
    /// </summary>
    public event Action<RocfTransferData>? OcfReceived;

    /// <summary>
    /// Event raised when a sync notification is received.
    /// </summary>
    public event Action<RocfSyncNotification>? SyncNotificationReceived;

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
    /// Initiates a START operation with OCF selection parameters.
    /// </summary>
    public async Task<bool> StartAsync(
        DateTime? startTime = null,
        DateTime? stopTime = null,
        RocfGlobalVcid? requestedGvcid = null,
        RocfControlWordType controlWordType = RocfControlWordType.All,
        int tcVcid = -1,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_state != SleServiceState.Ready)
                return false;

            _state = SleServiceState.StartPending;
            if (requestedGvcid != null)
                RequestedGlobalVcid = requestedGvcid;
            RequestedControlWordType = controlWordType;
            RequestedTcVcid = tcVcid;
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
    /// Processes a received OCF (called by transport layer).
    /// </summary>
    internal void ProcessOcf(RocfTransferData ocfData)
    {
        Statistics.OcfsReceived++;

        // Filter by control word type
        if (RequestedControlWordType != RocfControlWordType.All &&
            ocfData.ControlWordType != RequestedControlWordType)
        {
            Statistics.OcfsDiscarded++;
            return;
        }

        // Filter by TC VCID if CLCW
        if (ocfData.ControlWordType == RocfControlWordType.Clcw &&
            RequestedTcVcid >= 0 &&
            ocfData.TcVcid != RequestedTcVcid)
        {
            Statistics.OcfsDiscarded++;
            return;
        }

        Statistics.OcfsDelivered++;
        OcfReceived?.Invoke(ocfData);
    }
}

/// <summary>
/// ROCF global virtual channel identifier.
/// </summary>
public class RocfGlobalVcid
{
    /// <summary>
    /// Spacecraft identifier.
    /// </summary>
    public int SpacecraftId { get; set; }

    /// <summary>
    /// Transfer frame version number.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Virtual channel identifier.
    /// </summary>
    public int VirtualChannelId { get; set; }
}

/// <summary>
/// ROCF control word type filter.
/// </summary>
public enum RocfControlWordType
{
    /// <summary>
    /// All control word types.
    /// </summary>
    All,

    /// <summary>
    /// CLCW (Command Link Control Word) only.
    /// </summary>
    Clcw,

    /// <summary>
    /// Not CLCW (Type-2 reports).
    /// </summary>
    NotClcw
}

/// <summary>
/// ROCF transfer data (OCF delivery).
/// </summary>
public class RocfTransferData
{
    /// <summary>
    /// The OCF data (4 bytes).
    /// </summary>
    public byte[] OcfData { get; set; } = new byte[4];

    /// <summary>
    /// Earth receive time.
    /// </summary>
    public DateTime EarthReceiveTime { get; set; }

    /// <summary>
    /// Antenna ID.
    /// </summary>
    public string AntennaId { get; set; } = string.Empty;

    /// <summary>
    /// Global VCID for the containing frame.
    /// </summary>
    public RocfGlobalVcid GlobalVcid { get; set; } = new();

    /// <summary>
    /// Control word type.
    /// </summary>
    public RocfControlWordType ControlWordType { get; set; }

    /// <summary>
    /// TC virtual channel ID (if CLCW).
    /// </summary>
    public int TcVcid { get; set; }
}

/// <summary>
/// ROCF sync notification.
/// </summary>
public class RocfSyncNotification
{
    /// <summary>
    /// Notification type.
    /// </summary>
    public RocfNotificationType Type { get; set; }

    /// <summary>
    /// Production status if applicable.
    /// </summary>
    public RocfProductionStatus? ProductionStatus { get; set; }
}

/// <summary>
/// ROCF notification types.
/// </summary>
public enum RocfNotificationType
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
/// ROCF production status.
/// </summary>
public enum RocfProductionStatus
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
/// ROCF service statistics.
/// </summary>
public class RocfStatistics
{
    /// <summary>
    /// Number of OCFs received.
    /// </summary>
    public long OcfsReceived { get; set; }

    /// <summary>
    /// Number of OCFs delivered.
    /// </summary>
    public long OcfsDelivered { get; set; }

    /// <summary>
    /// Number of OCFs discarded.
    /// </summary>
    public long OcfsDiscarded { get; set; }
}

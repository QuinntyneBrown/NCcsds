namespace NCcsds.Sle.Common;

/// <summary>
/// SLE service instance states.
/// </summary>
public enum SleServiceState
{
    /// <summary>
    /// Service is not bound to a provider.
    /// </summary>
    Unbound,

    /// <summary>
    /// Bind operation is in progress.
    /// </summary>
    BindPending,

    /// <summary>
    /// Service is bound and ready.
    /// </summary>
    Ready,

    /// <summary>
    /// Start operation is in progress.
    /// </summary>
    StartPending,

    /// <summary>
    /// Service is actively transferring data.
    /// </summary>
    Active,

    /// <summary>
    /// Stop operation is in progress.
    /// </summary>
    StopPending,

    /// <summary>
    /// Unbind operation is in progress.
    /// </summary>
    UnbindPending
}

/// <summary>
/// SLE service types.
/// </summary>
public enum SleServiceType
{
    /// <summary>
    /// Return All Frames.
    /// </summary>
    Raf,

    /// <summary>
    /// Return Channel Frames.
    /// </summary>
    Rcf,

    /// <summary>
    /// Return Operational Control Field.
    /// </summary>
    Rocf,

    /// <summary>
    /// Command Link Transmission Unit.
    /// </summary>
    Cltu,

    /// <summary>
    /// Forward Space Packet.
    /// </summary>
    Fsp
}

/// <summary>
/// SLE protocol versions.
/// </summary>
public enum SleVersion
{
    /// <summary>
    /// SLE version 1.
    /// </summary>
    V1 = 1,

    /// <summary>
    /// SLE version 2.
    /// </summary>
    V2 = 2,

    /// <summary>
    /// SLE version 3.
    /// </summary>
    V3 = 3,

    /// <summary>
    /// SLE version 4.
    /// </summary>
    V4 = 4,

    /// <summary>
    /// SLE version 5.
    /// </summary>
    V5 = 5
}

/// <summary>
/// SLE bind diagnostic codes.
/// </summary>
public enum SleBindDiagnostic
{
    /// <summary>
    /// Access denied.
    /// </summary>
    AccessDenied = 0,

    /// <summary>
    /// Service type not supported.
    /// </summary>
    ServiceTypeNotSupported = 1,

    /// <summary>
    /// Version not supported.
    /// </summary>
    VersionNotSupported = 2,

    /// <summary>
    /// No such service instance.
    /// </summary>
    NoSuchServiceInstance = 3,

    /// <summary>
    /// Already bound.
    /// </summary>
    AlreadyBound = 4,

    /// <summary>
    /// SI not accessible to this initiator.
    /// </summary>
    SiNotAccessibleToThisInitiator = 5,

    /// <summary>
    /// Inconsistent service type.
    /// </summary>
    InconsistentServiceType = 6,

    /// <summary>
    /// Invalid time.
    /// </summary>
    InvalidTime = 7,

    /// <summary>
    /// Out of service.
    /// </summary>
    OutOfService = 8,

    /// <summary>
    /// Other reason.
    /// </summary>
    OtherReason = 127
}

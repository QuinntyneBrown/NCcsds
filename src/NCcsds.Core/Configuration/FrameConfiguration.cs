using NCcsds.Core.Identifiers;

namespace NCcsds.Core.Configuration;

/// <summary>
/// Configuration for TM (Telemetry) frame processing.
/// </summary>
public class TmFrameConfiguration
{
    /// <summary>
    /// The spacecraft identifier.
    /// </summary>
    public ushort SpacecraftId { get; set; }

    /// <summary>
    /// The frame length in bytes (including header and FECF if present).
    /// </summary>
    public int FrameLength { get; set; } = 1115;

    /// <summary>
    /// Whether the Frame Error Control Field (FECF) is present.
    /// </summary>
    public bool HasFecf { get; set; } = true;

    /// <summary>
    /// Whether the Operational Control Field is present.
    /// </summary>
    public bool HasOcf { get; set; } = true;

    /// <summary>
    /// Length of the secondary header, if present.
    /// </summary>
    public int SecondaryHeaderLength { get; set; }

    /// <summary>
    /// Whether frame randomization is applied.
    /// </summary>
    public bool IsRandomized { get; set; }

    /// <summary>
    /// Virtual channels configured for this spacecraft.
    /// </summary>
    public List<VirtualChannelConfiguration> VirtualChannels { get; set; } = new();
}

/// <summary>
/// Configuration for TC (Telecommand) frame processing.
/// </summary>
public class TcFrameConfiguration
{
    /// <summary>
    /// The spacecraft identifier.
    /// </summary>
    public ushort SpacecraftId { get; set; }

    /// <summary>
    /// Maximum frame length in bytes.
    /// </summary>
    public int MaxFrameLength { get; set; } = 1024;

    /// <summary>
    /// Whether the Frame Error Control Field (FECF) is present.
    /// </summary>
    public bool HasFecf { get; set; } = true;

    /// <summary>
    /// Whether COP-1 is enabled.
    /// </summary>
    public bool Cop1Enabled { get; set; } = true;

    /// <summary>
    /// Virtual channels configured for this spacecraft.
    /// </summary>
    public List<VirtualChannelConfiguration> VirtualChannels { get; set; } = new();
}

/// <summary>
/// Configuration for AOS frame processing.
/// </summary>
public class AosFrameConfiguration
{
    /// <summary>
    /// The spacecraft identifier.
    /// </summary>
    public ushort SpacecraftId { get; set; }

    /// <summary>
    /// The frame length in bytes.
    /// </summary>
    public int FrameLength { get; set; } = 1115;

    /// <summary>
    /// Whether the Frame Error Control Field (FECF) is present.
    /// </summary>
    public bool HasFecf { get; set; } = true;

    /// <summary>
    /// Length of the insert zone, if present.
    /// </summary>
    public int InsertZoneLength { get; set; }

    /// <summary>
    /// Whether frame randomization is applied.
    /// </summary>
    public bool IsRandomized { get; set; }

    /// <summary>
    /// Virtual channels configured for this spacecraft.
    /// </summary>
    public List<VirtualChannelConfiguration> VirtualChannels { get; set; } = new();
}

/// <summary>
/// Configuration for a virtual channel.
/// </summary>
public class VirtualChannelConfiguration
{
    /// <summary>
    /// The virtual channel identifier.
    /// </summary>
    public byte VirtualChannelId { get; set; }

    /// <summary>
    /// Name of the virtual channel.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether packets are extracted from this virtual channel.
    /// </summary>
    public bool ExtractPackets { get; set; } = true;

    /// <summary>
    /// Whether this is an idle virtual channel.
    /// </summary>
    public bool IsIdle { get; set; }
}

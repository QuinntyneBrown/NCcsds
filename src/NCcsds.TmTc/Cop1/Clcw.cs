using System.Buffers.Binary;

namespace NCcsds.TmTc.Cop1;

/// <summary>
/// Command Link Control Word (CLCW) for COP-1.
/// </summary>
public readonly struct Clcw : IEquatable<Clcw>
{
    /// <summary>
    /// CLCW size in bytes.
    /// </summary>
    public const int Size = 4;

    /// <summary>
    /// Control Word Type (0 = Type 1 CLCW).
    /// </summary>
    public byte ControlWordType { get; }

    /// <summary>
    /// CLCW Version Number (should be 0).
    /// </summary>
    public byte ClcwVersionNumber { get; }

    /// <summary>
    /// Status Field (3 bits, mission-specific).
    /// </summary>
    public byte StatusField { get; }

    /// <summary>
    /// COP in Effect (2 bits, 01 = COP-1).
    /// </summary>
    public byte CopInEffect { get; }

    /// <summary>
    /// Virtual Channel Identifier (6 bits).
    /// </summary>
    public byte VirtualChannelId { get; }

    /// <summary>
    /// Reserved (2 bits).
    /// </summary>
    public byte Reserved { get; }

    /// <summary>
    /// No RF Available flag.
    /// </summary>
    public bool NoRfAvailable { get; }

    /// <summary>
    /// No Bit Lock flag.
    /// </summary>
    public bool NoBitLock { get; }

    /// <summary>
    /// Lockout flag.
    /// </summary>
    public bool Lockout { get; }

    /// <summary>
    /// Wait flag.
    /// </summary>
    public bool Wait { get; }

    /// <summary>
    /// Retransmit flag.
    /// </summary>
    public bool Retransmit { get; }

    /// <summary>
    /// FARM-B Counter (2 bits).
    /// </summary>
    public byte FarmBCounter { get; }

    /// <summary>
    /// Report Value (Next Expected Frame Sequence Number).
    /// </summary>
    public byte ReportValue { get; }

    /// <summary>
    /// Creates a new CLCW.
    /// </summary>
    public Clcw(
        byte virtualChannelId,
        byte reportValue,
        bool lockout = false,
        bool wait = false,
        bool retransmit = false,
        bool noRfAvailable = false,
        bool noBitLock = false,
        byte statusField = 0,
        byte farmBCounter = 0)
    {
        ControlWordType = 0;
        ClcwVersionNumber = 0;
        StatusField = statusField;
        CopInEffect = 1; // COP-1
        VirtualChannelId = virtualChannelId;
        Reserved = 0;
        NoRfAvailable = noRfAvailable;
        NoBitLock = noBitLock;
        Lockout = lockout;
        Wait = wait;
        Retransmit = retransmit;
        FarmBCounter = farmBCounter;
        ReportValue = reportValue;
    }

    private Clcw(uint rawValue)
    {
        ControlWordType = (byte)((rawValue >> 31) & 0x01);
        ClcwVersionNumber = (byte)((rawValue >> 29) & 0x03);
        StatusField = (byte)((rawValue >> 26) & 0x07);
        CopInEffect = (byte)((rawValue >> 24) & 0x03);
        VirtualChannelId = (byte)((rawValue >> 18) & 0x3F);
        Reserved = (byte)((rawValue >> 16) & 0x03);
        NoRfAvailable = ((rawValue >> 15) & 0x01) != 0;
        NoBitLock = ((rawValue >> 14) & 0x01) != 0;
        Lockout = ((rawValue >> 13) & 0x01) != 0;
        Wait = ((rawValue >> 12) & 0x01) != 0;
        Retransmit = ((rawValue >> 11) & 0x01) != 0;
        FarmBCounter = (byte)((rawValue >> 9) & 0x03);
        ReportValue = (byte)(rawValue & 0xFF);
    }

    /// <summary>
    /// Gets whether FARM is ready to receive frames.
    /// </summary>
    public bool FarmReady => !Lockout && !Wait;

    /// <summary>
    /// Encodes this CLCW to a 32-bit value.
    /// </summary>
    public uint ToUInt32()
    {
        return (uint)(
            ((ControlWordType & 0x01) << 31) |
            ((ClcwVersionNumber & 0x03) << 29) |
            ((StatusField & 0x07) << 26) |
            ((CopInEffect & 0x03) << 24) |
            ((VirtualChannelId & 0x3F) << 18) |
            ((Reserved & 0x03) << 16) |
            (NoRfAvailable ? 0x8000 : 0) |
            (NoBitLock ? 0x4000 : 0) |
            (Lockout ? 0x2000 : 0) |
            (Wait ? 0x1000 : 0) |
            (Retransmit ? 0x0800 : 0) |
            ((FarmBCounter & 0x03) << 9) |
            (ReportValue & 0xFF)
        );
    }

    /// <summary>
    /// Encodes this CLCW to a byte span.
    /// </summary>
    public void Encode(Span<byte> destination)
    {
        if (destination.Length < Size)
            throw new ArgumentException("Destination too small.", nameof(destination));

        BinaryPrimitives.WriteUInt32BigEndian(destination, ToUInt32());
    }

    /// <summary>
    /// Decodes a CLCW from a 32-bit value.
    /// </summary>
    public static Clcw FromUInt32(uint value) => new(value);

    /// <summary>
    /// Decodes a CLCW from a byte span.
    /// </summary>
    public static Clcw Decode(ReadOnlySpan<byte> source)
    {
        if (source.Length < Size)
            throw new ArgumentException("Source too small.", nameof(source));

        return new Clcw(BinaryPrimitives.ReadUInt32BigEndian(source));
    }

    public bool Equals(Clcw other) => ToUInt32() == other.ToUInt32();
    public override bool Equals(object? obj) => obj is Clcw other && Equals(other);
    public override int GetHashCode() => ToUInt32().GetHashCode();

    public static bool operator ==(Clcw left, Clcw right) => left.Equals(right);
    public static bool operator !=(Clcw left, Clcw right) => !left.Equals(right);

    public override string ToString() =>
        $"CLCW(VC={VirtualChannelId}, Report={ReportValue}, Lock={Lockout}, Wait={Wait}, Retrans={Retransmit})";
}

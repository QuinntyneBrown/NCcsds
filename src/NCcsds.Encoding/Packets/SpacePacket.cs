using System.Buffers.Binary;
using NCcsds.Core;
using NCcsds.Core.Identifiers;

namespace NCcsds.Encoding.Packets;

/// <summary>
/// CCSDS Space Packet primary header and data.
/// </summary>
public class SpacePacket
{
    /// <summary>
    /// Primary header size in bytes.
    /// </summary>
    public const int PrimaryHeaderSize = 6;

    /// <summary>
    /// Maximum packet data length.
    /// </summary>
    public const int MaxDataLength = 65536;

    /// <summary>
    /// Packet version number (always 0 for CCSDS).
    /// </summary>
    public byte VersionNumber { get; set; }

    /// <summary>
    /// Packet type (0 = TM, 1 = TC).
    /// </summary>
    public PacketType Type { get; set; }

    /// <summary>
    /// Secondary header flag.
    /// </summary>
    public bool HasSecondaryHeader { get; set; }

    /// <summary>
    /// Application Process Identifier.
    /// </summary>
    public ApplicationProcessId Apid { get; set; }

    /// <summary>
    /// Sequence flags.
    /// </summary>
    public SequenceFlags SequenceFlags { get; set; }

    /// <summary>
    /// Packet sequence count or name.
    /// </summary>
    public ushort SequenceCount { get; set; }

    /// <summary>
    /// Packet data length (number of octets in data field - 1).
    /// </summary>
    public ushort DataLength { get; set; }

    /// <summary>
    /// Packet data (including secondary header if present).
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the total packet size.
    /// </summary>
    public int TotalSize => PrimaryHeaderSize + Data.Length;

    /// <summary>
    /// Gets whether this is an idle packet.
    /// </summary>
    public bool IsIdle => Apid.IsIdle;

    /// <summary>
    /// Creates a new empty space packet.
    /// </summary>
    public SpacePacket()
    {
    }

    /// <summary>
    /// Creates a new space packet with the specified parameters.
    /// </summary>
    public SpacePacket(PacketType type, ApplicationProcessId apid, SequenceFlags flags, ushort sequenceCount, byte[] data, bool hasSecondaryHeader = false)
    {
        VersionNumber = 0;
        Type = type;
        HasSecondaryHeader = hasSecondaryHeader;
        Apid = apid;
        SequenceFlags = flags;
        SequenceCount = sequenceCount;
        Data = data;
        DataLength = (ushort)(data.Length - 1);
    }

    /// <summary>
    /// Encodes this packet to a byte array.
    /// </summary>
    public byte[] Encode()
    {
        var buffer = new byte[TotalSize];
        Encode(buffer);
        return buffer;
    }

    /// <summary>
    /// Encodes this packet to a span.
    /// </summary>
    public int Encode(Span<byte> destination)
    {
        if (destination.Length < TotalSize)
            throw new ArgumentException("Destination too small.", nameof(destination));

        // First word: Version (3) | Type (1) | SecHdr (1) | APID (11)
        ushort word1 = (ushort)(
            ((VersionNumber & 0x07) << 13) |
            ((byte)Type << 12) |
            (HasSecondaryHeader ? 0x0800 : 0) |
            (Apid.Value & 0x07FF)
        );

        // Second word: SeqFlags (2) | SeqCount (14)
        ushort word2 = (ushort)(
            ((byte)SequenceFlags << 14) |
            (SequenceCount & 0x3FFF)
        );

        // Third word: Data Length
        ushort word3 = DataLength;

        BinaryPrimitives.WriteUInt16BigEndian(destination[0..2], word1);
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..4], word2);
        BinaryPrimitives.WriteUInt16BigEndian(destination[4..6], word3);

        Data.CopyTo(destination[6..]);

        return TotalSize;
    }

    /// <summary>
    /// Decodes a space packet from a span.
    /// </summary>
    public static SpacePacket Decode(ReadOnlySpan<byte> source)
    {
        if (source.Length < PrimaryHeaderSize)
            throw new ArgumentException("Source too small for header.", nameof(source));

        ushort word1 = BinaryPrimitives.ReadUInt16BigEndian(source[0..2]);
        ushort word2 = BinaryPrimitives.ReadUInt16BigEndian(source[2..4]);
        ushort word3 = BinaryPrimitives.ReadUInt16BigEndian(source[4..6]);

        var packet = new SpacePacket
        {
            VersionNumber = (byte)((word1 >> 13) & 0x07),
            Type = (PacketType)((word1 >> 12) & 0x01),
            HasSecondaryHeader = (word1 & 0x0800) != 0,
            Apid = new ApplicationProcessId((ushort)(word1 & 0x07FF)),
            SequenceFlags = (SequenceFlags)((word2 >> 14) & 0x03),
            SequenceCount = (ushort)(word2 & 0x3FFF),
            DataLength = word3
        };

        int dataSize = word3 + 1;
        if (source.Length < PrimaryHeaderSize + dataSize)
            throw new ArgumentException("Source too small for data field.", nameof(source));

        packet.Data = source.Slice(PrimaryHeaderSize, dataSize).ToArray();

        return packet;
    }

    /// <summary>
    /// Tries to decode a space packet from a span.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> source, out SpacePacket? packet, out int bytesConsumed)
    {
        packet = null;
        bytesConsumed = 0;

        if (source.Length < PrimaryHeaderSize)
            return false;

        ushort word3 = BinaryPrimitives.ReadUInt16BigEndian(source[4..6]);
        int expectedSize = PrimaryHeaderSize + word3 + 1;

        if (source.Length < expectedSize)
            return false;

        packet = Decode(source[..expectedSize]);
        bytesConsumed = expectedSize;
        return true;
    }
}

/// <summary>
/// Space packet type.
/// </summary>
public enum PacketType : byte
{
    /// <summary>
    /// Telemetry packet.
    /// </summary>
    Telemetry = 0,

    /// <summary>
    /// Telecommand packet.
    /// </summary>
    Telecommand = 1
}

/// <summary>
/// Space packet sequence flags.
/// </summary>
public enum SequenceFlags : byte
{
    /// <summary>
    /// Continuation segment.
    /// </summary>
    Continuation = 0,

    /// <summary>
    /// First segment of a group.
    /// </summary>
    FirstSegment = 1,

    /// <summary>
    /// Last segment of a group.
    /// </summary>
    LastSegment = 2,

    /// <summary>
    /// Unsegmented (standalone packet).
    /// </summary>
    Unsegmented = 3
}

using System.Buffers.Binary;
using NCcsds.Core.Identifiers;

namespace NCcsds.Encoding.Packets;

/// <summary>
/// ECSS PUS (Packet Utilization Standard) telemetry packet.
/// </summary>
public class PusTmPacket : SpacePacket
{
    /// <summary>
    /// PUS-C secondary header size (minimum).
    /// </summary>
    public const int SecondaryHeaderSize = 7;

    /// <summary>
    /// PUS version (1 for PUS-A, 2 for PUS-C).
    /// </summary>
    public byte PusVersion { get; set; } = 2;

    /// <summary>
    /// Spacecraft time reference status.
    /// </summary>
    public byte TimeReferenceStatus { get; set; }

    /// <summary>
    /// Service type.
    /// </summary>
    public byte ServiceType { get; set; }

    /// <summary>
    /// Service subtype.
    /// </summary>
    public byte ServiceSubtype { get; set; }

    /// <summary>
    /// Message type counter.
    /// </summary>
    public ushort MessageTypeCounter { get; set; }

    /// <summary>
    /// Destination ID.
    /// </summary>
    public ushort DestinationId { get; set; }

    /// <summary>
    /// Packet time (raw bytes).
    /// </summary>
    public byte[] Time { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Source data (application data after secondary header).
    /// </summary>
    public byte[] SourceData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Packet error control (CRC-16, if present).
    /// </summary>
    public ushort? PacketErrorControl { get; set; }

    /// <summary>
    /// Creates a new PUS TM packet.
    /// </summary>
    public PusTmPacket()
    {
        Type = PacketType.Telemetry;
        HasSecondaryHeader = true;
    }

    /// <summary>
    /// Encodes the PUS TM packet secondary header and data.
    /// </summary>
    public new byte[] Encode()
    {
        // Build the data field
        var dataField = new List<byte>();

        // PUS-C secondary header
        // Byte 1: PUS Version (4) | Spacecraft time reference status (4)
        dataField.Add((byte)((PusVersion << 4) | (TimeReferenceStatus & 0x0F)));

        // Byte 2: Service Type
        dataField.Add(ServiceType);

        // Byte 3: Service Subtype
        dataField.Add(ServiceSubtype);

        // Bytes 4-5: Message Type Counter
        dataField.Add((byte)(MessageTypeCounter >> 8));
        dataField.Add((byte)(MessageTypeCounter & 0xFF));

        // Bytes 6-7: Destination ID
        dataField.Add((byte)(DestinationId >> 8));
        dataField.Add((byte)(DestinationId & 0xFF));

        // Time field
        dataField.AddRange(Time);

        // Source data
        dataField.AddRange(SourceData);

        // Update the base packet's data
        Data = dataField.ToArray();
        DataLength = (ushort)(Data.Length - 1);

        // Add PEC if present
        if (PacketErrorControl.HasValue)
        {
            var result = new byte[TotalSize + 2];
            base.Encode(result.AsSpan(0, TotalSize));
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(TotalSize, 2), PacketErrorControl.Value);
            return result;
        }

        return base.Encode();
    }

    /// <summary>
    /// Decodes a PUS TM packet from a span.
    /// </summary>
    public static new PusTmPacket Decode(ReadOnlySpan<byte> source)
    {
        var basePacket = SpacePacket.Decode(source);

        if (!basePacket.HasSecondaryHeader)
            throw new ArgumentException("Packet does not have a secondary header.", nameof(source));

        if (basePacket.Data.Length < SecondaryHeaderSize)
            throw new ArgumentException("Data field too small for PUS secondary header.", nameof(source));

        var data = basePacket.Data.AsSpan();

        var packet = new PusTmPacket
        {
            VersionNumber = basePacket.VersionNumber,
            Type = basePacket.Type,
            HasSecondaryHeader = basePacket.HasSecondaryHeader,
            Apid = basePacket.Apid,
            SequenceFlags = basePacket.SequenceFlags,
            SequenceCount = basePacket.SequenceCount,
            DataLength = basePacket.DataLength,
            Data = basePacket.Data,

            PusVersion = (byte)((data[0] >> 4) & 0x0F),
            TimeReferenceStatus = (byte)(data[0] & 0x0F),
            ServiceType = data[1],
            ServiceSubtype = data[2],
            MessageTypeCounter = (ushort)((data[3] << 8) | data[4]),
            DestinationId = (ushort)((data[5] << 8) | data[6])
        };

        // Remaining data is time + source data (time length is mission-specific)
        if (data.Length > SecondaryHeaderSize)
        {
            packet.SourceData = data[SecondaryHeaderSize..].ToArray();
        }

        return packet;
    }
}

/// <summary>
/// ECSS PUS (Packet Utilization Standard) telecommand packet.
/// </summary>
public class PusTcPacket : SpacePacket
{
    /// <summary>
    /// PUS-C TC secondary header size (minimum).
    /// </summary>
    public const int SecondaryHeaderSize = 5;

    /// <summary>
    /// PUS version (1 for PUS-A, 2 for PUS-C).
    /// </summary>
    public byte PusVersion { get; set; } = 2;

    /// <summary>
    /// Acknowledgement flags.
    /// </summary>
    public AckFlags AcknowledgementFlags { get; set; }

    /// <summary>
    /// Service type.
    /// </summary>
    public byte ServiceType { get; set; }

    /// <summary>
    /// Service subtype.
    /// </summary>
    public byte ServiceSubtype { get; set; }

    /// <summary>
    /// Source ID.
    /// </summary>
    public ushort SourceId { get; set; }

    /// <summary>
    /// Application data.
    /// </summary>
    public byte[] ApplicationData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Packet error control (CRC-16, if present).
    /// </summary>
    public ushort? PacketErrorControl { get; set; }

    /// <summary>
    /// Creates a new PUS TC packet.
    /// </summary>
    public PusTcPacket()
    {
        Type = PacketType.Telecommand;
        HasSecondaryHeader = true;
    }

    /// <summary>
    /// Encodes the PUS TC packet.
    /// </summary>
    public new byte[] Encode()
    {
        var dataField = new List<byte>();

        // Byte 1: PUS Version (4) | Ack flags (4)
        dataField.Add((byte)((PusVersion << 4) | ((byte)AcknowledgementFlags & 0x0F)));

        // Byte 2: Service Type
        dataField.Add(ServiceType);

        // Byte 3: Service Subtype
        dataField.Add(ServiceSubtype);

        // Bytes 4-5: Source ID
        dataField.Add((byte)(SourceId >> 8));
        dataField.Add((byte)(SourceId & 0xFF));

        // Application data
        dataField.AddRange(ApplicationData);

        Data = dataField.ToArray();
        DataLength = (ushort)(Data.Length - 1);

        if (PacketErrorControl.HasValue)
        {
            var result = new byte[TotalSize + 2];
            base.Encode(result.AsSpan(0, TotalSize));
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(TotalSize, 2), PacketErrorControl.Value);
            return result;
        }

        return base.Encode();
    }

    /// <summary>
    /// Decodes a PUS TC packet from a span.
    /// </summary>
    public static new PusTcPacket Decode(ReadOnlySpan<byte> source)
    {
        var basePacket = SpacePacket.Decode(source);

        if (!basePacket.HasSecondaryHeader)
            throw new ArgumentException("Packet does not have a secondary header.", nameof(source));

        if (basePacket.Data.Length < SecondaryHeaderSize)
            throw new ArgumentException("Data field too small for PUS secondary header.", nameof(source));

        var data = basePacket.Data.AsSpan();

        var packet = new PusTcPacket
        {
            VersionNumber = basePacket.VersionNumber,
            Type = basePacket.Type,
            HasSecondaryHeader = basePacket.HasSecondaryHeader,
            Apid = basePacket.Apid,
            SequenceFlags = basePacket.SequenceFlags,
            SequenceCount = basePacket.SequenceCount,
            DataLength = basePacket.DataLength,
            Data = basePacket.Data,

            PusVersion = (byte)((data[0] >> 4) & 0x0F),
            AcknowledgementFlags = (AckFlags)(data[0] & 0x0F),
            ServiceType = data[1],
            ServiceSubtype = data[2],
            SourceId = (ushort)((data[3] << 8) | data[4])
        };

        if (data.Length > SecondaryHeaderSize)
        {
            packet.ApplicationData = data[SecondaryHeaderSize..].ToArray();
        }

        return packet;
    }
}

/// <summary>
/// PUS acknowledgement flags.
/// </summary>
[Flags]
public enum AckFlags : byte
{
    /// <summary>
    /// No acknowledgement requested.
    /// </summary>
    None = 0,

    /// <summary>
    /// Acceptance acknowledgement.
    /// </summary>
    Acceptance = 1,

    /// <summary>
    /// Start of execution acknowledgement.
    /// </summary>
    Start = 2,

    /// <summary>
    /// Progress of execution acknowledgement.
    /// </summary>
    Progress = 4,

    /// <summary>
    /// Completion of execution acknowledgement.
    /// </summary>
    Completion = 8,

    /// <summary>
    /// All acknowledgements.
    /// </summary>
    All = Acceptance | Start | Progress | Completion
}

namespace NCcsds.Cfdp.Pdu;

/// <summary>
/// CFDP PDU header.
/// </summary>
public class PduHeader
{
    /// <summary>
    /// PDU version (3 bits).
    /// </summary>
    public byte Version { get; set; } = 1;

    /// <summary>
    /// PDU type.
    /// </summary>
    public PduType Type { get; set; }

    /// <summary>
    /// Direction of PDU transmission.
    /// </summary>
    public PduDirection Direction { get; set; }

    /// <summary>
    /// Transmission mode.
    /// </summary>
    public TransmissionMode TransmissionMode { get; set; }

    /// <summary>
    /// CRC flag indicating if PDU contains a CRC.
    /// </summary>
    public bool CrcPresent { get; set; }

    /// <summary>
    /// Large file flag (extended file size fields).
    /// </summary>
    public bool LargeFileFlag { get; set; }

    /// <summary>
    /// PDU data field length.
    /// </summary>
    public int DataFieldLength { get; set; }

    /// <summary>
    /// Segmentation control.
    /// </summary>
    public SegmentationControl SegmentationControl { get; set; }

    /// <summary>
    /// Length of entity IDs in bytes.
    /// </summary>
    public int EntityIdLength { get; set; } = 2;

    /// <summary>
    /// Segment metadata flag.
    /// </summary>
    public bool SegmentMetadataFlag { get; set; }

    /// <summary>
    /// Length of sequence number in bytes.
    /// </summary>
    public int SequenceNumberLength { get; set; } = 2;

    /// <summary>
    /// Source entity ID.
    /// </summary>
    public ulong SourceEntityId { get; set; }

    /// <summary>
    /// Transaction sequence number.
    /// </summary>
    public ulong TransactionSequenceNumber { get; set; }

    /// <summary>
    /// Destination entity ID.
    /// </summary>
    public ulong DestinationEntityId { get; set; }

    /// <summary>
    /// Gets the header length in bytes.
    /// </summary>
    public int HeaderLength => 4 + EntityIdLength + SequenceNumberLength + EntityIdLength;

    /// <summary>
    /// Encodes the PDU header.
    /// </summary>
    public byte[] Encode()
    {
        var header = new byte[HeaderLength];
        int offset = 0;

        // First byte: version (3), type (1), direction (1), mode (1), CRC (1), large file (1)
        header[offset] = (byte)((Version << 5) |
                                ((int)Type << 4) |
                                ((int)Direction << 3) |
                                ((int)TransmissionMode << 2) |
                                (CrcPresent ? 0x02 : 0x00) |
                                (LargeFileFlag ? 0x01 : 0x00));
        offset++;

        // Data field length (2 bytes)
        header[offset++] = (byte)(DataFieldLength >> 8);
        header[offset++] = (byte)DataFieldLength;

        // Fourth byte: segmentation control (1), entity ID length (3), segment metadata (1), sequence number length (3)
        header[offset] = (byte)(((int)SegmentationControl << 7) |
                                ((EntityIdLength - 1) << 4) |
                                (SegmentMetadataFlag ? 0x08 : 0x00) |
                                (SequenceNumberLength - 1));
        offset++;

        // Source entity ID
        WriteVariableLength(header, ref offset, SourceEntityId, EntityIdLength);

        // Transaction sequence number
        WriteVariableLength(header, ref offset, TransactionSequenceNumber, SequenceNumberLength);

        // Destination entity ID
        WriteVariableLength(header, ref offset, DestinationEntityId, EntityIdLength);

        return header;
    }

    /// <summary>
    /// Decodes a PDU header from bytes.
    /// </summary>
    public static PduHeader Decode(ReadOnlySpan<byte> data, out int bytesConsumed)
    {
        if (data.Length < 4)
            throw new FormatException("Insufficient data for PDU header");

        var header = new PduHeader();
        int offset = 0;

        // First byte
        header.Version = (byte)((data[offset] >> 5) & 0x07);
        header.Type = (PduType)((data[offset] >> 4) & 0x01);
        header.Direction = (PduDirection)((data[offset] >> 3) & 0x01);
        header.TransmissionMode = (TransmissionMode)((data[offset] >> 2) & 0x01);
        header.CrcPresent = (data[offset] & 0x02) != 0;
        header.LargeFileFlag = (data[offset] & 0x01) != 0;
        offset++;

        // Data field length
        header.DataFieldLength = (data[offset] << 8) | data[offset + 1];
        offset += 2;

        // Fourth byte
        header.SegmentationControl = (SegmentationControl)((data[offset] >> 7) & 0x01);
        header.EntityIdLength = ((data[offset] >> 4) & 0x07) + 1;
        header.SegmentMetadataFlag = (data[offset] & 0x08) != 0;
        header.SequenceNumberLength = (data[offset] & 0x07) + 1;
        offset++;

        var requiredLength = 4 + header.EntityIdLength + header.SequenceNumberLength + header.EntityIdLength;
        if (data.Length < requiredLength)
            throw new FormatException("Insufficient data for PDU header");

        // Source entity ID
        header.SourceEntityId = ReadVariableLength(data, ref offset, header.EntityIdLength);

        // Transaction sequence number
        header.TransactionSequenceNumber = ReadVariableLength(data, ref offset, header.SequenceNumberLength);

        // Destination entity ID
        header.DestinationEntityId = ReadVariableLength(data, ref offset, header.EntityIdLength);

        bytesConsumed = offset;
        return header;
    }

    private static void WriteVariableLength(byte[] buffer, ref int offset, ulong value, int length)
    {
        for (int i = length - 1; i >= 0; i--)
        {
            buffer[offset + i] = (byte)(value & 0xFF);
            value >>= 8;
        }
        offset += length;
    }

    private static ulong ReadVariableLength(ReadOnlySpan<byte> data, ref int offset, int length)
    {
        ulong value = 0;
        for (int i = 0; i < length; i++)
        {
            value = (value << 8) | data[offset + i];
        }
        offset += length;
        return value;
    }
}

/// <summary>
/// CFDP PDU type.
/// </summary>
public enum PduType
{
    /// <summary>
    /// File directive PDU.
    /// </summary>
    FileDirective = 0,

    /// <summary>
    /// File data PDU.
    /// </summary>
    FileData = 1
}

/// <summary>
/// CFDP PDU direction.
/// </summary>
public enum PduDirection
{
    /// <summary>
    /// Toward receiver.
    /// </summary>
    TowardReceiver = 0,

    /// <summary>
    /// Toward sender.
    /// </summary>
    TowardSender = 1
}

/// <summary>
/// CFDP transmission mode.
/// </summary>
public enum TransmissionMode
{
    /// <summary>
    /// Acknowledged (Class 2).
    /// </summary>
    Acknowledged = 0,

    /// <summary>
    /// Unacknowledged (Class 1).
    /// </summary>
    Unacknowledged = 1
}

/// <summary>
/// CFDP segmentation control.
/// </summary>
public enum SegmentationControl
{
    /// <summary>
    /// Record boundaries not respected.
    /// </summary>
    NotRespected = 0,

    /// <summary>
    /// Record boundaries respected.
    /// </summary>
    Respected = 1
}

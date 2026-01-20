namespace NCcsds.Cfdp.Pdu;

/// <summary>
/// File data PDU.
/// </summary>
public class FileDataPdu
{
    /// <summary>
    /// PDU header.
    /// </summary>
    public PduHeader Header { get; set; } = new() { Type = PduType.FileData };

    /// <summary>
    /// Segment metadata (optional).
    /// </summary>
    public SegmentMetadata? SegmentMetadata { get; set; }

    /// <summary>
    /// Offset in file where this data starts.
    /// </summary>
    public ulong Offset { get; set; }

    /// <summary>
    /// File data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Encodes the PDU.
    /// </summary>
    public byte[] Encode()
    {
        var offsetLength = Header.LargeFileFlag ? 8 : 4;
        var segmentMetadataLength = Header.SegmentMetadataFlag && SegmentMetadata != null
            ? 1 + SegmentMetadata.Metadata.Length
            : 0;

        var dataFieldLength = segmentMetadataLength + offsetLength + Data.Length;
        Header.DataFieldLength = dataFieldLength;

        var header = Header.Encode();
        var pdu = new byte[header.Length + dataFieldLength];
        var offset = 0;

        // Header
        header.CopyTo(pdu, offset);
        offset += header.Length;

        // Segment metadata if present
        if (Header.SegmentMetadataFlag && SegmentMetadata != null)
        {
            pdu[offset++] = (byte)((SegmentMetadata.RecordContinuationState << 6) | SegmentMetadata.Metadata.Length);
            SegmentMetadata.Metadata.CopyTo(pdu, offset);
            offset += SegmentMetadata.Metadata.Length;
        }

        // Offset
        if (Header.LargeFileFlag)
        {
            for (int i = 7; i >= 0; i--)
                pdu[offset++] = (byte)(Offset >> (i * 8));
        }
        else
        {
            for (int i = 3; i >= 0; i--)
                pdu[offset++] = (byte)(Offset >> (i * 8));
        }

        // Data
        Data.CopyTo(pdu, offset);

        return pdu;
    }

    /// <summary>
    /// Decodes a file data PDU.
    /// </summary>
    public static FileDataPdu Decode(PduHeader header, ReadOnlySpan<byte> data)
    {
        var pdu = new FileDataPdu { Header = header };
        int offset = 0;

        // Segment metadata if present
        if (header.SegmentMetadataFlag)
        {
            var metaByte = data[offset++];
            var continuationState = (metaByte >> 6) & 0x03;
            var metaLength = metaByte & 0x3F;
            pdu.SegmentMetadata = new SegmentMetadata
            {
                RecordContinuationState = continuationState,
                Metadata = data.Slice(offset, metaLength).ToArray()
            };
            offset += metaLength;
        }

        // Offset
        var offsetLength = header.LargeFileFlag ? 8 : 4;
        pdu.Offset = 0;
        for (int i = 0; i < offsetLength; i++)
            pdu.Offset = (pdu.Offset << 8) | data[offset++];

        // Data
        pdu.Data = data[offset..].ToArray();

        return pdu;
    }
}

/// <summary>
/// Segment metadata for file data PDUs.
/// </summary>
public class SegmentMetadata
{
    /// <summary>
    /// Record continuation state.
    /// 0 = not applicable
    /// 1 = start of record
    /// 2 = middle of record
    /// 3 = end of record
    /// </summary>
    public int RecordContinuationState { get; set; }

    /// <summary>
    /// Application-specific metadata.
    /// </summary>
    public byte[] Metadata { get; set; } = Array.Empty<byte>();
}

namespace NCcsds.Cfdp.Pdu;

/// <summary>
/// File directive codes.
/// </summary>
public enum DirectiveCode : byte
{
    /// <summary>
    /// End of file.
    /// </summary>
    Eof = 0x04,

    /// <summary>
    /// Finished.
    /// </summary>
    Finished = 0x05,

    /// <summary>
    /// Acknowledgment.
    /// </summary>
    Ack = 0x06,

    /// <summary>
    /// Metadata.
    /// </summary>
    Metadata = 0x07,

    /// <summary>
    /// Negative acknowledgment.
    /// </summary>
    Nak = 0x08,

    /// <summary>
    /// Prompt.
    /// </summary>
    Prompt = 0x09,

    /// <summary>
    /// Keep alive.
    /// </summary>
    KeepAlive = 0x0C
}

/// <summary>
/// Base class for file directive PDUs.
/// </summary>
public abstract class FileDirectivePdu
{
    /// <summary>
    /// PDU header.
    /// </summary>
    public PduHeader Header { get; set; } = new() { Type = PduType.FileDirective };

    /// <summary>
    /// Directive code.
    /// </summary>
    public abstract DirectiveCode DirectiveCode { get; }

    /// <summary>
    /// Encodes the directive-specific content.
    /// </summary>
    protected abstract byte[] EncodeContent();

    /// <summary>
    /// Encodes the complete PDU.
    /// </summary>
    public byte[] Encode()
    {
        var content = EncodeContent();
        var data = new byte[1 + content.Length];
        data[0] = (byte)DirectiveCode;
        content.CopyTo(data, 1);

        Header.DataFieldLength = data.Length;
        var header = Header.Encode();

        var pdu = new byte[header.Length + data.Length];
        header.CopyTo(pdu, 0);
        data.CopyTo(pdu, header.Length);

        return pdu;
    }
}

/// <summary>
/// Metadata PDU.
/// </summary>
public class MetadataPdu : FileDirectivePdu
{
    /// <inheritdoc />
    public override DirectiveCode DirectiveCode => DirectiveCode.Metadata;

    /// <summary>
    /// Closure requested flag.
    /// </summary>
    public bool ClosureRequested { get; set; }

    /// <summary>
    /// Checksum type.
    /// </summary>
    public ChecksumType ChecksumType { get; set; } = ChecksumType.Modular;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public ulong FileSize { get; set; }

    /// <summary>
    /// Source file name.
    /// </summary>
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>
    /// Destination file name.
    /// </summary>
    public string DestinationFileName { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override byte[] EncodeContent()
    {
        var srcNameBytes = System.Text.Encoding.ASCII.GetBytes(SourceFileName);
        var dstNameBytes = System.Text.Encoding.ASCII.GetBytes(DestinationFileName);

        var fileSizeLength = Header.LargeFileFlag ? 8 : 4;
        var length = 1 + fileSizeLength + 1 + srcNameBytes.Length + 1 + dstNameBytes.Length;
        var data = new byte[length];
        int offset = 0;

        // First byte: closure requested (1), checksum type (4), reserved (3)
        data[offset++] = (byte)((ClosureRequested ? 0x40 : 0x00) | ((int)ChecksumType & 0x0F));

        // File size
        if (Header.LargeFileFlag)
        {
            for (int i = 7; i >= 0; i--)
                data[offset++] = (byte)(FileSize >> (i * 8));
        }
        else
        {
            for (int i = 3; i >= 0; i--)
                data[offset++] = (byte)(FileSize >> (i * 8));
        }

        // Source file name
        data[offset++] = (byte)srcNameBytes.Length;
        srcNameBytes.CopyTo(data, offset);
        offset += srcNameBytes.Length;

        // Destination file name
        data[offset++] = (byte)dstNameBytes.Length;
        dstNameBytes.CopyTo(data, offset);

        return data;
    }

    /// <summary>
    /// Decodes a Metadata PDU from data.
    /// </summary>
    public static MetadataPdu Decode(PduHeader header, ReadOnlySpan<byte> data)
    {
        var pdu = new MetadataPdu { Header = header };
        int offset = 0;

        // Skip directive code
        offset++;

        // First byte
        pdu.ClosureRequested = (data[offset] & 0x40) != 0;
        pdu.ChecksumType = (ChecksumType)(data[offset] & 0x0F);
        offset++;

        // File size
        var fileSizeLength = header.LargeFileFlag ? 8 : 4;
        pdu.FileSize = 0;
        for (int i = 0; i < fileSizeLength; i++)
            pdu.FileSize = (pdu.FileSize << 8) | data[offset++];

        // Source file name
        var srcNameLength = data[offset++];
        pdu.SourceFileName = System.Text.Encoding.ASCII.GetString(data.Slice(offset, srcNameLength));
        offset += srcNameLength;

        // Destination file name
        var dstNameLength = data[offset++];
        pdu.DestinationFileName = System.Text.Encoding.ASCII.GetString(data.Slice(offset, dstNameLength));

        return pdu;
    }
}

/// <summary>
/// EOF (End of File) PDU.
/// </summary>
public class EofPdu : FileDirectivePdu
{
    /// <inheritdoc />
    public override DirectiveCode DirectiveCode => DirectiveCode.Eof;

    /// <summary>
    /// Condition code.
    /// </summary>
    public ConditionCode ConditionCode { get; set; } = ConditionCode.NoError;

    /// <summary>
    /// File checksum.
    /// </summary>
    public uint Checksum { get; set; }

    /// <summary>
    /// File size.
    /// </summary>
    public ulong FileSize { get; set; }

    /// <summary>
    /// Fault location (only if condition code != no error).
    /// </summary>
    public ulong? FaultLocation { get; set; }

    /// <inheritdoc />
    protected override byte[] EncodeContent()
    {
        var fileSizeLength = Header.LargeFileFlag ? 8 : 4;
        var hasFault = ConditionCode != ConditionCode.NoError && FaultLocation.HasValue;
        var length = 1 + 4 + fileSizeLength + (hasFault ? Header.EntityIdLength : 0);
        var data = new byte[length];
        int offset = 0;

        // Condition code (4 bits) + spare (4 bits)
        data[offset++] = (byte)((int)ConditionCode << 4);

        // Checksum
        data[offset++] = (byte)(Checksum >> 24);
        data[offset++] = (byte)(Checksum >> 16);
        data[offset++] = (byte)(Checksum >> 8);
        data[offset++] = (byte)Checksum;

        // File size
        if (Header.LargeFileFlag)
        {
            for (int i = 7; i >= 0; i--)
                data[offset++] = (byte)(FileSize >> (i * 8));
        }
        else
        {
            for (int i = 3; i >= 0; i--)
                data[offset++] = (byte)(FileSize >> (i * 8));
        }

        // Fault location if applicable
        if (hasFault)
        {
            var fault = FaultLocation!.Value;
            for (int i = Header.EntityIdLength - 1; i >= 0; i--)
                data[offset++] = (byte)(fault >> (i * 8));
        }

        return data;
    }

    /// <summary>
    /// Decodes an EOF PDU from data.
    /// </summary>
    public static EofPdu Decode(PduHeader header, ReadOnlySpan<byte> data)
    {
        var pdu = new EofPdu { Header = header };
        int offset = 1; // Skip directive code

        // Condition code
        pdu.ConditionCode = (ConditionCode)((data[offset++] >> 4) & 0x0F);

        // Checksum
        pdu.Checksum = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                              (data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        // File size
        var fileSizeLength = header.LargeFileFlag ? 8 : 4;
        pdu.FileSize = 0;
        for (int i = 0; i < fileSizeLength; i++)
            pdu.FileSize = (pdu.FileSize << 8) | data[offset++];

        return pdu;
    }
}

/// <summary>
/// Finished PDU.
/// </summary>
public class FinishedPdu : FileDirectivePdu
{
    /// <inheritdoc />
    public override DirectiveCode DirectiveCode => DirectiveCode.Finished;

    /// <summary>
    /// Condition code.
    /// </summary>
    public ConditionCode ConditionCode { get; set; } = ConditionCode.NoError;

    /// <summary>
    /// Whether file was delivered.
    /// </summary>
    public bool DeliveryCode { get; set; }

    /// <summary>
    /// File status.
    /// </summary>
    public FileStatus FileStatus { get; set; } = FileStatus.Unreported;

    /// <inheritdoc />
    protected override byte[] EncodeContent()
    {
        return [(byte)(((int)ConditionCode << 4) | (DeliveryCode ? 0x04 : 0x00) | ((int)FileStatus & 0x03))];
    }

    /// <summary>
    /// Decodes a Finished PDU from data.
    /// </summary>
    public static FinishedPdu Decode(PduHeader header, ReadOnlySpan<byte> data)
    {
        var pdu = new FinishedPdu { Header = header };
        int offset = 1; // Skip directive code

        pdu.ConditionCode = (ConditionCode)((data[offset] >> 4) & 0x0F);
        pdu.DeliveryCode = (data[offset] & 0x04) != 0;
        pdu.FileStatus = (FileStatus)(data[offset] & 0x03);

        return pdu;
    }
}

/// <summary>
/// ACK (Acknowledgment) PDU.
/// </summary>
public class AckPdu : FileDirectivePdu
{
    /// <inheritdoc />
    public override DirectiveCode DirectiveCode => DirectiveCode.Ack;

    /// <summary>
    /// Directive code being acknowledged.
    /// </summary>
    public DirectiveCode AcknowledgedDirective { get; set; }

    /// <summary>
    /// Directive subtype code.
    /// </summary>
    public byte DirectiveSubtypeCode { get; set; }

    /// <summary>
    /// Condition code.
    /// </summary>
    public ConditionCode ConditionCode { get; set; } = ConditionCode.NoError;

    /// <summary>
    /// Transaction status.
    /// </summary>
    public TransactionStatus TransactionStatus { get; set; }

    /// <inheritdoc />
    protected override byte[] EncodeContent()
    {
        return [
            (byte)(((int)AcknowledgedDirective << 4) | (DirectiveSubtypeCode & 0x0F)),
            (byte)(((int)ConditionCode << 4) | ((int)TransactionStatus & 0x03))
        ];
    }

    /// <summary>
    /// Decodes an ACK PDU from data.
    /// </summary>
    public static AckPdu Decode(PduHeader header, ReadOnlySpan<byte> data)
    {
        var pdu = new AckPdu { Header = header };
        int offset = 1; // Skip directive code

        pdu.AcknowledgedDirective = (DirectiveCode)((data[offset] >> 4) & 0x0F);
        pdu.DirectiveSubtypeCode = (byte)(data[offset] & 0x0F);
        offset++;

        pdu.ConditionCode = (ConditionCode)((data[offset] >> 4) & 0x0F);
        pdu.TransactionStatus = (TransactionStatus)(data[offset] & 0x03);

        return pdu;
    }
}

/// <summary>
/// NAK (Negative Acknowledgment) PDU.
/// </summary>
public class NakPdu : FileDirectivePdu
{
    /// <inheritdoc />
    public override DirectiveCode DirectiveCode => DirectiveCode.Nak;

    /// <summary>
    /// Start of scope.
    /// </summary>
    public ulong StartOfScope { get; set; }

    /// <summary>
    /// End of scope.
    /// </summary>
    public ulong EndOfScope { get; set; }

    /// <summary>
    /// Segment requests (list of offset/length pairs).
    /// </summary>
    public List<SegmentRequest> SegmentRequests { get; set; } = new();

    /// <inheritdoc />
    protected override byte[] EncodeContent()
    {
        var offsetLength = Header.LargeFileFlag ? 8 : 4;
        var length = offsetLength * 2 + SegmentRequests.Count * offsetLength * 2;
        var data = new byte[length];
        int offset = 0;

        // Start of scope
        WriteOffset(data, ref offset, StartOfScope, offsetLength);

        // End of scope
        WriteOffset(data, ref offset, EndOfScope, offsetLength);

        // Segment requests
        foreach (var segment in SegmentRequests)
        {
            WriteOffset(data, ref offset, segment.StartOffset, offsetLength);
            WriteOffset(data, ref offset, segment.EndOffset, offsetLength);
        }

        return data;
    }

    private static void WriteOffset(byte[] data, ref int offset, ulong value, int length)
    {
        for (int i = length - 1; i >= 0; i--)
            data[offset++] = (byte)(value >> (i * 8));
    }

    /// <summary>
    /// Decodes a NAK PDU from data.
    /// </summary>
    public static NakPdu Decode(PduHeader header, ReadOnlySpan<byte> data)
    {
        var pdu = new NakPdu { Header = header };
        var offsetLength = header.LargeFileFlag ? 8 : 4;
        int offset = 1; // Skip directive code

        pdu.StartOfScope = ReadOffset(data, ref offset, offsetLength);
        pdu.EndOfScope = ReadOffset(data, ref offset, offsetLength);

        while (offset + offsetLength * 2 <= data.Length)
        {
            var start = ReadOffset(data, ref offset, offsetLength);
            var end = ReadOffset(data, ref offset, offsetLength);
            pdu.SegmentRequests.Add(new SegmentRequest { StartOffset = start, EndOffset = end });
        }

        return pdu;
    }

    private static ulong ReadOffset(ReadOnlySpan<byte> data, ref int offset, int length)
    {
        ulong value = 0;
        for (int i = 0; i < length; i++)
            value = (value << 8) | data[offset++];
        return value;
    }
}

/// <summary>
/// Segment request in NAK PDU.
/// </summary>
public class SegmentRequest
{
    /// <summary>
    /// Start offset.
    /// </summary>
    public ulong StartOffset { get; set; }

    /// <summary>
    /// End offset.
    /// </summary>
    public ulong EndOffset { get; set; }
}

/// <summary>
/// CFDP checksum types.
/// </summary>
public enum ChecksumType : byte
{
    /// <summary>
    /// Modular checksum.
    /// </summary>
    Modular = 0,

    /// <summary>
    /// CRC-32.
    /// </summary>
    Crc32 = 1,

    /// <summary>
    /// CRC-32C.
    /// </summary>
    Crc32C = 2,

    /// <summary>
    /// Null checksum (no verification).
    /// </summary>
    Null = 15
}

/// <summary>
/// CFDP condition codes.
/// </summary>
public enum ConditionCode : byte
{
    NoError = 0,
    PositiveAckLimitReached = 1,
    KeepAliveLimitReached = 2,
    InvalidTransmissionMode = 3,
    FilestoreRejection = 4,
    FileChecksumFailure = 5,
    FileSizeError = 6,
    NakLimitReached = 7,
    InactivityDetected = 8,
    InvalidFileStructure = 9,
    CheckLimitReached = 10,
    UnsupportedChecksumType = 11,
    SuspendRequestReceived = 14,
    CancelRequestReceived = 15
}

/// <summary>
/// CFDP file status.
/// </summary>
public enum FileStatus : byte
{
    /// <summary>
    /// Discarded deliberately.
    /// </summary>
    DiscardedDeliberately = 0,

    /// <summary>
    /// Discarded due to filestore rejection.
    /// </summary>
    DiscardedFilestoreRejection = 1,

    /// <summary>
    /// Retained in filestore successfully.
    /// </summary>
    RetainedSuccessfully = 2,

    /// <summary>
    /// Status unreported.
    /// </summary>
    Unreported = 3
}

/// <summary>
/// CFDP transaction status.
/// </summary>
public enum TransactionStatus : byte
{
    Undefined = 0,
    Active = 1,
    Terminated = 2,
    Unrecognized = 3
}

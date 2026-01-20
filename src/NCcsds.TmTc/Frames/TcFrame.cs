using System.Buffers.Binary;
using NCcsds.Core.Checksums;
using NCcsds.Core.Identifiers;

namespace NCcsds.TmTc.Frames;

/// <summary>
/// CCSDS TC (Telecommand) Transfer Frame.
/// </summary>
public class TcFrame
{
    /// <summary>
    /// Primary header size in bytes.
    /// </summary>
    public const int PrimaryHeaderSize = 5;

    /// <summary>
    /// Frame Error Control Field size in bytes.
    /// </summary>
    public const int FecfSize = 2;

    /// <summary>
    /// Transfer Frame Version Number (2 bits, always 00 for TC).
    /// </summary>
    public TransferFrameVersionNumber VersionNumber { get; set; } = TransferFrameVersionNumber.TC;

    /// <summary>
    /// Bypass Flag (1 = Type-B, 0 = Type-A).
    /// </summary>
    public bool BypassFlag { get; set; }

    /// <summary>
    /// Control Command Flag (1 = control command, 0 = data command).
    /// </summary>
    public bool ControlCommandFlag { get; set; }

    /// <summary>
    /// Reserved field (2 bits, should be 00).
    /// </summary>
    public byte Reserved { get; set; }

    /// <summary>
    /// Spacecraft Identifier (10 bits).
    /// </summary>
    public SpacecraftId SpacecraftId { get; set; }

    /// <summary>
    /// Virtual Channel Identifier (6 bits for TC).
    /// </summary>
    public VirtualChannelId VirtualChannelId { get; set; }

    /// <summary>
    /// Frame Length (10 bits, actual length - 1).
    /// </summary>
    public ushort FrameLength { get; set; }

    /// <summary>
    /// Frame Sequence Number (8 bits).
    /// </summary>
    public byte FrameSequenceNumber { get; set; }

    /// <summary>
    /// Frame data field.
    /// </summary>
    public byte[] DataField { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Frame Error Control Field (CRC-16).
    /// </summary>
    public ushort? Fecf { get; set; }

    /// <summary>
    /// Gets the total frame size.
    /// </summary>
    public int TotalSize => PrimaryHeaderSize + DataField.Length + (Fecf.HasValue ? FecfSize : 0);

    /// <summary>
    /// Gets whether this is a Type-B (bypass) frame.
    /// </summary>
    public bool IsTypeBFrame => BypassFlag;

    /// <summary>
    /// Gets whether this is a Type-A (sequence-controlled) frame.
    /// </summary>
    public bool IsTypeAFrame => !BypassFlag;

    /// <summary>
    /// Encodes this frame to a byte array.
    /// </summary>
    public byte[] Encode()
    {
        var buffer = new byte[TotalSize];
        Encode(buffer);
        return buffer;
    }

    /// <summary>
    /// Encodes this frame to a span.
    /// </summary>
    public int Encode(Span<byte> destination)
    {
        if (destination.Length < TotalSize)
            throw new ArgumentException("Destination too small.", nameof(destination));

        int offset = 0;

        // First 2 bytes: VersionNumber (2) | Bypass (1) | CC (1) | Reserved (2) | SCID (10)
        ushort word1 = (ushort)(
            ((VersionNumber.Value & 0x03) << 14) |
            (BypassFlag ? 0x2000 : 0) |
            (ControlCommandFlag ? 0x1000 : 0) |
            ((Reserved & 0x03) << 10) |
            (SpacecraftId.Value & 0x3FF)
        );
        BinaryPrimitives.WriteUInt16BigEndian(destination[offset..], word1);
        offset += 2;

        // Byte 3-4: VCID (6) | Frame Length (10)
        ushort word2 = (ushort)(
            ((VirtualChannelId.Value & 0x3F) << 10) |
            (FrameLength & 0x3FF)
        );
        BinaryPrimitives.WriteUInt16BigEndian(destination[offset..], word2);
        offset += 2;

        // Byte 5: Frame Sequence Number
        destination[offset++] = FrameSequenceNumber;

        // Data field
        DataField.CopyTo(destination[offset..]);
        offset += DataField.Length;

        // FECF (if present)
        if (Fecf.HasValue)
        {
            var crc = Crc16Ccitt.Compute(destination[..offset]);
            BinaryPrimitives.WriteUInt16BigEndian(destination[offset..], crc);
            offset += 2;
        }

        return offset;
    }

    /// <summary>
    /// Decodes a TC frame from a span.
    /// </summary>
    public static TcFrame Decode(ReadOnlySpan<byte> source, bool hasFecf = true)
    {
        if (source.Length < PrimaryHeaderSize)
            throw new ArgumentException("Source too small for header.", nameof(source));

        var frame = new TcFrame();
        int offset = 0;

        // Parse first 2 bytes
        ushort word1 = BinaryPrimitives.ReadUInt16BigEndian(source[offset..]);
        offset += 2;

        frame.VersionNumber = new TransferFrameVersionNumber((byte)((word1 >> 14) & 0x03));
        frame.BypassFlag = (word1 & 0x2000) != 0;
        frame.ControlCommandFlag = (word1 & 0x1000) != 0;
        frame.Reserved = (byte)((word1 >> 10) & 0x03);
        frame.SpacecraftId = new SpacecraftId((ushort)(word1 & 0x3FF));

        // Parse bytes 3-4
        ushort word2 = BinaryPrimitives.ReadUInt16BigEndian(source[offset..]);
        offset += 2;

        frame.VirtualChannelId = new VirtualChannelId((byte)((word2 >> 10) & 0x3F));
        frame.FrameLength = (ushort)(word2 & 0x3FF);

        // Parse frame sequence number
        frame.FrameSequenceNumber = source[offset++];

        // Calculate data field size
        int dataEnd = source.Length - (hasFecf ? FecfSize : 0);
        frame.DataField = source[offset..dataEnd].ToArray();

        // Parse FECF
        if (hasFecf)
        {
            frame.Fecf = BinaryPrimitives.ReadUInt16BigEndian(source[(source.Length - FecfSize)..]);
        }

        return frame;
    }

    /// <summary>
    /// Validates the frame's FECF.
    /// </summary>
    public bool ValidateFecf(ReadOnlySpan<byte> rawFrame)
    {
        if (!Fecf.HasValue)
            return true;

        return Crc16Ccitt.Validate(rawFrame);
    }
}

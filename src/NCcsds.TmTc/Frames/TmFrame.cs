using System.Buffers.Binary;
using NCcsds.Core;
using NCcsds.Core.Checksums;
using NCcsds.Core.Identifiers;

namespace NCcsds.TmTc.Frames;

/// <summary>
/// CCSDS TM (Telemetry) Transfer Frame.
/// </summary>
public class TmFrame
{
    /// <summary>
    /// Primary header size in bytes.
    /// </summary>
    public const int PrimaryHeaderSize = 6;

    /// <summary>
    /// Frame Error Control Field size in bytes.
    /// </summary>
    public const int FecfSize = 2;

    /// <summary>
    /// Operational Control Field size in bytes.
    /// </summary>
    public const int OcfSize = 4;

    /// <summary>
    /// Transfer Frame Version Number (2 bits, always 00 for TM).
    /// </summary>
    public TransferFrameVersionNumber VersionNumber { get; set; } = TransferFrameVersionNumber.TM;

    /// <summary>
    /// Spacecraft Identifier (10 bits).
    /// </summary>
    public SpacecraftId SpacecraftId { get; set; }

    /// <summary>
    /// Virtual Channel Identifier (3 bits).
    /// </summary>
    public VirtualChannelId VirtualChannelId { get; set; }

    /// <summary>
    /// Operational Control Field Flag.
    /// </summary>
    public bool OcfFlag { get; set; }

    /// <summary>
    /// Master Channel Frame Count (8 bits).
    /// </summary>
    public byte MasterChannelFrameCount { get; set; }

    /// <summary>
    /// Virtual Channel Frame Count (8 bits).
    /// </summary>
    public byte VirtualChannelFrameCount { get; set; }

    /// <summary>
    /// Transfer Frame Secondary Header Flag.
    /// </summary>
    public bool SecondaryHeaderFlag { get; set; }

    /// <summary>
    /// Synchronization Flag.
    /// </summary>
    public bool SynchronizationFlag { get; set; }

    /// <summary>
    /// Packet Order Flag.
    /// </summary>
    public bool PacketOrderFlag { get; set; }

    /// <summary>
    /// Segment Length Identifier (2 bits).
    /// </summary>
    public byte SegmentLengthId { get; set; }

    /// <summary>
    /// First Header Pointer (11 bits).
    /// </summary>
    public ushort FirstHeaderPointer { get; set; }

    /// <summary>
    /// Secondary header data (if present).
    /// </summary>
    public byte[]? SecondaryHeader { get; set; }

    /// <summary>
    /// Frame data field.
    /// </summary>
    public byte[] DataField { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Operational Control Field (if present).
    /// </summary>
    public uint? Ocf { get; set; }

    /// <summary>
    /// Frame Error Control Field (CRC-16).
    /// </summary>
    public ushort? Fecf { get; set; }

    /// <summary>
    /// Gets the total frame size.
    /// </summary>
    public int TotalSize
    {
        get
        {
            int size = PrimaryHeaderSize;
            if (SecondaryHeader != null) size += SecondaryHeader.Length;
            size += DataField.Length;
            if (Ocf.HasValue) size += OcfSize;
            if (Fecf.HasValue) size += FecfSize;
            return size;
        }
    }

    /// <summary>
    /// Value for First Header Pointer when no packet starts in this frame.
    /// </summary>
    public const ushort FhpNoPacketStart = 0x7FE;

    /// <summary>
    /// Value for First Header Pointer when only idle data is present.
    /// </summary>
    public const ushort FhpIdleData = 0x7FF;

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

        // First 2 bytes: VersionNumber (2) | SCID (10) | VCID (3) | OCF Flag (1)
        ushort word1 = (ushort)(
            ((VersionNumber.Value & 0x03) << 14) |
            ((SpacecraftId.Value & 0x3FF) << 4) |
            ((VirtualChannelId.Value & 0x07) << 1) |
            (OcfFlag ? 1 : 0)
        );
        BinaryPrimitives.WriteUInt16BigEndian(destination[offset..], word1);
        offset += 2;

        // Byte 3: Master Channel Frame Count
        destination[offset++] = MasterChannelFrameCount;

        // Byte 4: Virtual Channel Frame Count
        destination[offset++] = VirtualChannelFrameCount;

        // Bytes 5-6: TF Data Field Status
        ushort tfStatus = (ushort)(
            (SecondaryHeaderFlag ? 0x8000 : 0) |
            (SynchronizationFlag ? 0x4000 : 0) |
            (PacketOrderFlag ? 0x2000 : 0) |
            ((SegmentLengthId & 0x03) << 11) |
            (FirstHeaderPointer & 0x7FF)
        );
        BinaryPrimitives.WriteUInt16BigEndian(destination[offset..], tfStatus);
        offset += 2;

        // Secondary header (if present)
        if (SecondaryHeader != null)
        {
            SecondaryHeader.CopyTo(destination[offset..]);
            offset += SecondaryHeader.Length;
        }

        // Data field
        DataField.CopyTo(destination[offset..]);
        offset += DataField.Length;

        // OCF (if present)
        if (Ocf.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination[offset..], Ocf.Value);
            offset += 4;
        }

        // FECF (if present)
        if (Fecf.HasValue)
        {
            // Calculate and write CRC
            var crc = Crc16Ccitt.Compute(destination[..(offset)]);
            BinaryPrimitives.WriteUInt16BigEndian(destination[offset..], crc);
            offset += 2;
        }

        return offset;
    }

    /// <summary>
    /// Decodes a TM frame from a span.
    /// </summary>
    public static TmFrame Decode(ReadOnlySpan<byte> source, bool hasFecf = true, bool hasOcf = true, int secondaryHeaderLength = 0)
    {
        if (source.Length < PrimaryHeaderSize)
            throw new ArgumentException("Source too small for header.", nameof(source));

        var frame = new TmFrame();
        int offset = 0;

        // Parse first 2 bytes
        ushort word1 = BinaryPrimitives.ReadUInt16BigEndian(source[offset..]);
        offset += 2;

        frame.VersionNumber = new TransferFrameVersionNumber((byte)((word1 >> 14) & 0x03));
        frame.SpacecraftId = new SpacecraftId((ushort)((word1 >> 4) & 0x3FF));
        frame.VirtualChannelId = new VirtualChannelId((byte)((word1 >> 1) & 0x07));
        frame.OcfFlag = (word1 & 0x01) != 0;

        // Parse frame counts
        frame.MasterChannelFrameCount = source[offset++];
        frame.VirtualChannelFrameCount = source[offset++];

        // Parse TF Data Field Status
        ushort tfStatus = BinaryPrimitives.ReadUInt16BigEndian(source[offset..]);
        offset += 2;

        frame.SecondaryHeaderFlag = (tfStatus & 0x8000) != 0;
        frame.SynchronizationFlag = (tfStatus & 0x4000) != 0;
        frame.PacketOrderFlag = (tfStatus & 0x2000) != 0;
        frame.SegmentLengthId = (byte)((tfStatus >> 11) & 0x03);
        frame.FirstHeaderPointer = (ushort)(tfStatus & 0x7FF);

        // Calculate data field size
        int trailerSize = (hasFecf ? FecfSize : 0) + (hasOcf ? OcfSize : 0);
        int dataStart = offset + secondaryHeaderLength;
        int dataEnd = source.Length - trailerSize;

        // Parse secondary header
        if (secondaryHeaderLength > 0)
        {
            frame.SecondaryHeader = source.Slice(offset, secondaryHeaderLength).ToArray();
            offset += secondaryHeaderLength;
        }

        // Parse data field
        frame.DataField = source[dataStart..dataEnd].ToArray();

        // Parse OCF
        if (hasOcf)
        {
            frame.Ocf = BinaryPrimitives.ReadUInt32BigEndian(source[(source.Length - trailerSize)..]);
        }

        // Parse and validate FECF
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

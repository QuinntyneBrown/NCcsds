using System.Buffers.Binary;
using NCcsds.Core.Checksums;
using NCcsds.Core.Identifiers;

namespace NCcsds.TmTc.Frames;

/// <summary>
/// CCSDS AOS (Advanced Orbiting Systems) Transfer Frame.
/// </summary>
public class AosFrame
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
    /// Transfer Frame Version Number (2 bits, always 01 for AOS).
    /// </summary>
    public TransferFrameVersionNumber VersionNumber { get; set; } = TransferFrameVersionNumber.AOS;

    /// <summary>
    /// Spacecraft Identifier (8 bits for AOS).
    /// </summary>
    public byte SpacecraftId { get; set; }

    /// <summary>
    /// Virtual Channel Identifier (6 bits).
    /// </summary>
    public VirtualChannelId VirtualChannelId { get; set; }

    /// <summary>
    /// Virtual Channel Frame Count (24 bits).
    /// </summary>
    public uint VirtualChannelFrameCount { get; set; }

    /// <summary>
    /// Replay Flag.
    /// </summary>
    public bool ReplayFlag { get; set; }

    /// <summary>
    /// Virtual Channel Frame Count Usage Flag.
    /// </summary>
    public bool VcFrameCountUsageFlag { get; set; }

    /// <summary>
    /// Reserved (2 bits).
    /// </summary>
    public byte Reserved { get; set; }

    /// <summary>
    /// Virtual Channel Frame Count Cycle (4 bits).
    /// </summary>
    public byte VcFrameCountCycle { get; set; }

    /// <summary>
    /// Insert zone data (if present).
    /// </summary>
    public byte[]? InsertZone { get; set; }

    /// <summary>
    /// Frame data zone.
    /// </summary>
    public byte[] DataZone { get; set; } = Array.Empty<byte>();

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
            if (InsertZone != null) size += InsertZone.Length;
            size += DataZone.Length;
            if (Ocf.HasValue) size += 4;
            if (Fecf.HasValue) size += FecfSize;
            return size;
        }
    }

    /// <summary>
    /// First Header Pointer indicating no packet starts in this frame.
    /// </summary>
    public const ushort FhpNoPacketStart = 0x7FE;

    /// <summary>
    /// First Header Pointer indicating idle data only.
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

        // First 2 bytes: VersionNumber (2) | SCID (8) | VCID (6)
        ushort word1 = (ushort)(
            ((VersionNumber.Value & 0x03) << 14) |
            ((SpacecraftId & 0xFF) << 6) |
            (VirtualChannelId.Value & 0x3F)
        );
        BinaryPrimitives.WriteUInt16BigEndian(destination[offset..], word1);
        offset += 2;

        // Bytes 3-5: Virtual Channel Frame Count (24 bits)
        destination[offset++] = (byte)((VirtualChannelFrameCount >> 16) & 0xFF);
        destination[offset++] = (byte)((VirtualChannelFrameCount >> 8) & 0xFF);
        destination[offset++] = (byte)(VirtualChannelFrameCount & 0xFF);

        // Byte 6: Signaling Field
        byte signaling = (byte)(
            (ReplayFlag ? 0x80 : 0) |
            (VcFrameCountUsageFlag ? 0x40 : 0) |
            ((Reserved & 0x03) << 4) |
            (VcFrameCountCycle & 0x0F)
        );
        destination[offset++] = signaling;

        // Insert zone (if present)
        if (InsertZone != null)
        {
            InsertZone.CopyTo(destination[offset..]);
            offset += InsertZone.Length;
        }

        // Data zone
        DataZone.CopyTo(destination[offset..]);
        offset += DataZone.Length;

        // OCF (if present)
        if (Ocf.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination[offset..], Ocf.Value);
            offset += 4;
        }

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
    /// Decodes an AOS frame from a span.
    /// </summary>
    public static AosFrame Decode(ReadOnlySpan<byte> source, bool hasFecf = true, bool hasOcf = false, int insertZoneLength = 0)
    {
        if (source.Length < PrimaryHeaderSize)
            throw new ArgumentException("Source too small for header.", nameof(source));

        var frame = new AosFrame();
        int offset = 0;

        // Parse first 2 bytes
        ushort word1 = BinaryPrimitives.ReadUInt16BigEndian(source[offset..]);
        offset += 2;

        frame.VersionNumber = new TransferFrameVersionNumber((byte)((word1 >> 14) & 0x03));
        frame.SpacecraftId = (byte)((word1 >> 6) & 0xFF);
        frame.VirtualChannelId = new VirtualChannelId((byte)(word1 & 0x3F));

        // Parse VC Frame Count (24 bits)
        frame.VirtualChannelFrameCount = (uint)(
            (source[offset] << 16) |
            (source[offset + 1] << 8) |
            source[offset + 2]
        );
        offset += 3;

        // Parse signaling field
        byte signaling = source[offset++];
        frame.ReplayFlag = (signaling & 0x80) != 0;
        frame.VcFrameCountUsageFlag = (signaling & 0x40) != 0;
        frame.Reserved = (byte)((signaling >> 4) & 0x03);
        frame.VcFrameCountCycle = (byte)(signaling & 0x0F);

        // Calculate data zone size
        int trailerSize = (hasFecf ? FecfSize : 0) + (hasOcf ? 4 : 0);
        int dataStart = offset + insertZoneLength;
        int dataEnd = source.Length - trailerSize;

        // Parse insert zone
        if (insertZoneLength > 0)
        {
            frame.InsertZone = source.Slice(offset, insertZoneLength).ToArray();
            offset += insertZoneLength;
        }

        // Parse data zone
        frame.DataZone = source[dataStart..dataEnd].ToArray();

        // Parse OCF
        if (hasOcf)
        {
            frame.Ocf = BinaryPrimitives.ReadUInt32BigEndian(source[(source.Length - trailerSize)..]);
        }

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

using System.Buffers.Binary;
using NCcsds.Core;

namespace NCcsds.Encoding.Time;

/// <summary>
/// CCSDS Unsegmented Code (CUC) time format encoder/decoder.
/// </summary>
public readonly struct CucTime : IEquatable<CucTime>
{
    /// <summary>
    /// Coarse time (seconds since epoch).
    /// </summary>
    public uint CoarseTime { get; }

    /// <summary>
    /// Fine time (fractional seconds, resolution depends on configuration).
    /// </summary>
    public uint FineTime { get; }

    /// <summary>
    /// Number of octets for coarse time (1-4).
    /// </summary>
    public byte CoarseOctets { get; }

    /// <summary>
    /// Number of octets for fine time (0-3).
    /// </summary>
    public byte FineOctets { get; }

    /// <summary>
    /// Gets the total encoded size in bytes.
    /// </summary>
    public int EncodedSize => CoarseOctets + FineOctets;

    /// <summary>
    /// Creates a new CUC time value.
    /// </summary>
    public CucTime(uint coarseTime, uint fineTime, byte coarseOctets = 4, byte fineOctets = 2)
    {
        if (coarseOctets < 1 || coarseOctets > 4)
            throw new ArgumentOutOfRangeException(nameof(coarseOctets), "Coarse octets must be 1-4.");
        if (fineOctets > 3)
            throw new ArgumentOutOfRangeException(nameof(fineOctets), "Fine octets must be 0-3.");

        CoarseTime = coarseTime;
        FineTime = fineTime;
        CoarseOctets = coarseOctets;
        FineOctets = fineOctets;
    }

    /// <summary>
    /// Creates a CUC time from a DateTime.
    /// </summary>
    public static CucTime FromDateTime(DateTime dateTime, DateTime epoch, byte coarseOctets = 4, byte fineOctets = 2)
    {
        var elapsed = dateTime.ToUniversalTime() - epoch;
        var totalSeconds = elapsed.TotalSeconds;

        if (totalSeconds < 0)
            throw new ArgumentException("DateTime is before epoch.", nameof(dateTime));

        uint coarse = (uint)totalSeconds;
        double fractional = totalSeconds - coarse;

        // Fine time is fractional * 2^(fineOctets*8)
        uint fine = fineOctets > 0 ? (uint)(fractional * (1UL << (fineOctets * 8))) : 0;

        return new CucTime(coarse, fine, coarseOctets, fineOctets);
    }

    /// <summary>
    /// Converts this CUC time to a DateTime.
    /// </summary>
    public DateTime ToDateTime(DateTime epoch)
    {
        double fractional = FineOctets > 0 ? (double)FineTime / (1UL << (FineOctets * 8)) : 0;
        return epoch.AddSeconds(CoarseTime + fractional);
    }

    /// <summary>
    /// Encodes this CUC time to a byte span.
    /// </summary>
    public int Encode(Span<byte> destination)
    {
        if (destination.Length < EncodedSize)
            throw new ArgumentException("Destination too small.", nameof(destination));

        int offset = 0;

        // Encode coarse time (big-endian)
        for (int i = CoarseOctets - 1; i >= 0; i--)
        {
            destination[offset++] = (byte)(CoarseTime >> (i * 8));
        }

        // Encode fine time (big-endian, most significant bits first)
        for (int i = FineOctets - 1; i >= 0; i--)
        {
            destination[offset++] = (byte)(FineTime >> (i * 8));
        }

        return offset;
    }

    /// <summary>
    /// Decodes a CUC time from a byte span.
    /// </summary>
    public static CucTime Decode(ReadOnlySpan<byte> source, byte coarseOctets = 4, byte fineOctets = 2)
    {
        if (source.Length < coarseOctets + fineOctets)
            throw new ArgumentException("Source too small.", nameof(source));

        int offset = 0;

        // Decode coarse time
        uint coarse = 0;
        for (int i = 0; i < coarseOctets; i++)
        {
            coarse = (coarse << 8) | source[offset++];
        }

        // Decode fine time
        uint fine = 0;
        for (int i = 0; i < fineOctets; i++)
        {
            fine = (fine << 8) | source[offset++];
        }

        return new CucTime(coarse, fine, coarseOctets, fineOctets);
    }

    /// <summary>
    /// Generates a P-field for this CUC time configuration.
    /// </summary>
    public byte GeneratePField(bool hasExtension = false)
    {
        // P-field format: 0CCCFFEE
        // C = coarse octets - 1 (0-3 for 1-4 octets)
        // F = fine octets (0-3)
        // E = extension (01 for CUC level 1)
        byte pfield = (byte)(((CoarseOctets - 1) << 5) | (FineOctets << 2) | 0x01);
        if (hasExtension)
            pfield |= 0x80;
        return pfield;
    }

    public bool Equals(CucTime other) =>
        CoarseTime == other.CoarseTime &&
        FineTime == other.FineTime &&
        CoarseOctets == other.CoarseOctets &&
        FineOctets == other.FineOctets;

    public override bool Equals(object? obj) => obj is CucTime other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(CoarseTime, FineTime, CoarseOctets, FineOctets);

    public static bool operator ==(CucTime left, CucTime right) => left.Equals(right);
    public static bool operator !=(CucTime left, CucTime right) => !left.Equals(right);

    public override string ToString() => $"CUC({CoarseTime}.{FineTime})";
}

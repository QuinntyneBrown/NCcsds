namespace NCcsds.Encoding.Time;

/// <summary>
/// CCSDS Day Segmented (CDS) time format encoder/decoder.
/// </summary>
public readonly struct CdsTime : IEquatable<CdsTime>
{
    /// <summary>
    /// Day count since epoch.
    /// </summary>
    public ushort Days { get; }

    /// <summary>
    /// Milliseconds of day.
    /// </summary>
    public uint MillisecondsOfDay { get; }

    /// <summary>
    /// Microseconds within millisecond (0-999), optional.
    /// </summary>
    public ushort? Microseconds { get; }

    /// <summary>
    /// Whether the day segment uses 24 bits (vs 16 bits).
    /// </summary>
    public bool ExtendedDays { get; }

    /// <summary>
    /// Gets the total encoded size in bytes.
    /// </summary>
    public int EncodedSize => (ExtendedDays ? 3 : 2) + 4 + (Microseconds.HasValue ? 2 : 0);

    /// <summary>
    /// Creates a new CDS time value.
    /// </summary>
    public CdsTime(ushort days, uint millisecondsOfDay, ushort? microseconds = null, bool extendedDays = false)
    {
        if (millisecondsOfDay >= 86400000)
            throw new ArgumentOutOfRangeException(nameof(millisecondsOfDay), "Must be less than 86400000.");
        if (microseconds.HasValue && microseconds.Value >= 1000)
            throw new ArgumentOutOfRangeException(nameof(microseconds), "Must be less than 1000.");

        Days = days;
        MillisecondsOfDay = millisecondsOfDay;
        Microseconds = microseconds;
        ExtendedDays = extendedDays;
    }

    /// <summary>
    /// Creates a CDS time from a DateTime.
    /// </summary>
    public static CdsTime FromDateTime(DateTime dateTime, DateTime epoch, bool includeMicroseconds = true, bool extendedDays = false)
    {
        CcsdsTime.ToDayAndMillis(dateTime, epoch, out int days, out uint millis);

        if (!extendedDays && days > ushort.MaxValue)
            throw new ArgumentException("Day count exceeds 16-bit range. Use extendedDays=true.", nameof(dateTime));

        ushort? micros = null;
        if (includeMicroseconds)
        {
            var fractionalMillis = (dateTime.ToUniversalTime() - epoch).TotalMilliseconds;
            fractionalMillis -= Math.Floor(fractionalMillis);
            micros = (ushort)(fractionalMillis * 1000);
        }

        return new CdsTime((ushort)days, millis, micros, extendedDays);
    }

    /// <summary>
    /// Converts this CDS time to a DateTime.
    /// </summary>
    public DateTime ToDateTime(DateTime epoch)
    {
        var result = CcsdsTime.FromDayAndMillis(epoch, Days, MillisecondsOfDay);
        if (Microseconds.HasValue)
            result = result.AddTicks(Microseconds.Value * 10); // 1 microsecond = 10 ticks
        return result;
    }

    /// <summary>
    /// Encodes this CDS time to a byte span.
    /// </summary>
    public int Encode(Span<byte> destination)
    {
        if (destination.Length < EncodedSize)
            throw new ArgumentException("Destination too small.", nameof(destination));

        int offset = 0;

        // Encode days
        if (ExtendedDays)
        {
            destination[offset++] = (byte)(Days >> 16);
        }
        destination[offset++] = (byte)(Days >> 8);
        destination[offset++] = (byte)(Days & 0xFF);

        // Encode milliseconds of day (4 bytes)
        destination[offset++] = (byte)(MillisecondsOfDay >> 24);
        destination[offset++] = (byte)(MillisecondsOfDay >> 16);
        destination[offset++] = (byte)(MillisecondsOfDay >> 8);
        destination[offset++] = (byte)(MillisecondsOfDay & 0xFF);

        // Encode microseconds if present
        if (Microseconds.HasValue)
        {
            destination[offset++] = (byte)(Microseconds.Value >> 8);
            destination[offset++] = (byte)(Microseconds.Value & 0xFF);
        }

        return offset;
    }

    /// <summary>
    /// Decodes a CDS time from a byte span.
    /// </summary>
    public static CdsTime Decode(ReadOnlySpan<byte> source, bool hasMicroseconds = true, bool extendedDays = false)
    {
        int expectedSize = (extendedDays ? 3 : 2) + 4 + (hasMicroseconds ? 2 : 0);
        if (source.Length < expectedSize)
            throw new ArgumentException("Source too small.", nameof(source));

        int offset = 0;

        // Decode days
        ushort days;
        if (extendedDays)
        {
            days = (ushort)((source[offset] << 16) | (source[offset + 1] << 8) | source[offset + 2]);
            offset += 3;
        }
        else
        {
            days = (ushort)((source[offset] << 8) | source[offset + 1]);
            offset += 2;
        }

        // Decode milliseconds of day
        uint millis = (uint)((source[offset] << 24) | (source[offset + 1] << 16) |
                             (source[offset + 2] << 8) | source[offset + 3]);
        offset += 4;

        // Decode microseconds if present
        ushort? micros = null;
        if (hasMicroseconds)
        {
            micros = (ushort)((source[offset] << 8) | source[offset + 1]);
        }

        return new CdsTime(days, millis, micros, extendedDays);
    }

    /// <summary>
    /// Generates a P-field for this CDS time configuration.
    /// </summary>
    public byte GeneratePField()
    {
        // P-field format: 01AAATTT
        // A = agency defined epoch (000 for CCSDS epoch)
        // T = time code (100 for CDS level 1)
        byte pfield = 0x40; // CDS identification

        if (ExtendedDays)
            pfield |= 0x04;

        if (Microseconds.HasValue)
            pfield |= 0x02;

        return pfield;
    }

    public bool Equals(CdsTime other) =>
        Days == other.Days &&
        MillisecondsOfDay == other.MillisecondsOfDay &&
        Microseconds == other.Microseconds &&
        ExtendedDays == other.ExtendedDays;

    public override bool Equals(object? obj) => obj is CdsTime other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Days, MillisecondsOfDay, Microseconds, ExtendedDays);

    public static bool operator ==(CdsTime left, CdsTime right) => left.Equals(right);
    public static bool operator !=(CdsTime left, CdsTime right) => !left.Equals(right);

    public override string ToString() => $"CDS({Days}d+{MillisecondsOfDay}ms)";
}

using NCcsds.Core.Buffers;

namespace NCcsds.Encoding.Primitives;

/// <summary>
/// Encoder/decoder for integer values with arbitrary bit widths.
/// </summary>
public static class IntegerEncoder
{
    /// <summary>
    /// Encodes an unsigned integer with the specified bit width.
    /// </summary>
    /// <param name="writer">The bit writer.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="bitWidth">The bit width (1-64).</param>
    public static void EncodeUnsigned(ref BitWriter writer, ulong value, int bitWidth)
    {
        if (bitWidth < 1 || bitWidth > 64)
            throw new ArgumentOutOfRangeException(nameof(bitWidth), "Bit width must be between 1 and 64.");

        // Validate value fits in bit width
        if (bitWidth < 64 && value >= (1UL << bitWidth))
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} does not fit in {bitWidth} bits.");

        if (bitWidth <= 32)
        {
            writer.WriteBits((uint)value, bitWidth);
        }
        else
        {
            // Write high bits first (big-endian)
            int highBits = bitWidth - 32;
            writer.WriteBits((uint)(value >> 32), highBits);
            writer.WriteBits((uint)(value & 0xFFFFFFFF), 32);
        }
    }

    /// <summary>
    /// Decodes an unsigned integer with the specified bit width.
    /// </summary>
    /// <param name="reader">The bit reader.</param>
    /// <param name="bitWidth">The bit width (1-64).</param>
    /// <returns>The decoded value.</returns>
    public static ulong DecodeUnsigned(ref BitReader reader, int bitWidth)
    {
        if (bitWidth < 1 || bitWidth > 64)
            throw new ArgumentOutOfRangeException(nameof(bitWidth), "Bit width must be between 1 and 64.");

        if (bitWidth <= 32)
        {
            return reader.ReadBits(bitWidth);
        }
        else
        {
            int highBits = bitWidth - 32;
            ulong high = reader.ReadBits(highBits);
            ulong low = reader.ReadBits(32);
            return (high << 32) | low;
        }
    }

    /// <summary>
    /// Encodes a signed integer with the specified bit width (two's complement).
    /// </summary>
    /// <param name="writer">The bit writer.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="bitWidth">The bit width (1-64).</param>
    public static void EncodeSigned(ref BitWriter writer, long value, int bitWidth)
    {
        if (bitWidth < 1 || bitWidth > 64)
            throw new ArgumentOutOfRangeException(nameof(bitWidth), "Bit width must be between 1 and 64.");

        // Check if value fits in the specified bit width
        long minValue = -(1L << (bitWidth - 1));
        long maxValue = (1L << (bitWidth - 1)) - 1;
        if (value < minValue || value > maxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} does not fit in {bitWidth} signed bits.");

        EncodeUnsigned(ref writer, (ulong)value, bitWidth);
    }

    /// <summary>
    /// Decodes a signed integer with the specified bit width (two's complement).
    /// </summary>
    /// <param name="reader">The bit reader.</param>
    /// <param name="bitWidth">The bit width (1-64).</param>
    /// <returns>The decoded value.</returns>
    public static long DecodeSigned(ref BitReader reader, int bitWidth)
    {
        if (bitWidth < 1 || bitWidth > 64)
            throw new ArgumentOutOfRangeException(nameof(bitWidth), "Bit width must be between 1 and 64.");

        ulong unsigned = DecodeUnsigned(ref reader, bitWidth);

        // Sign extend
        if (bitWidth < 64 && (unsigned & (1UL << (bitWidth - 1))) != 0)
        {
            unsigned |= ~((1UL << bitWidth) - 1);
        }

        return (long)unsigned;
    }

    /// <summary>
    /// Encodes an unsigned integer to a byte span (big-endian).
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="byteCount">The number of bytes to write.</param>
    /// <param name="destination">The destination span.</param>
    public static void EncodeToBytes(ulong value, int byteCount, Span<byte> destination)
    {
        if (byteCount < 1 || byteCount > 8)
            throw new ArgumentOutOfRangeException(nameof(byteCount), "Byte count must be between 1 and 8.");
        if (destination.Length < byteCount)
            throw new ArgumentException("Destination too small.", nameof(destination));

        for (int i = byteCount - 1; i >= 0; i--)
        {
            destination[i] = (byte)(value & 0xFF);
            value >>= 8;
        }
    }

    /// <summary>
    /// Decodes an unsigned integer from a byte span (big-endian).
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="byteCount">The number of bytes to read.</param>
    /// <returns>The decoded value.</returns>
    public static ulong DecodeFromBytes(ReadOnlySpan<byte> source, int byteCount)
    {
        if (byteCount < 1 || byteCount > 8)
            throw new ArgumentOutOfRangeException(nameof(byteCount), "Byte count must be between 1 and 8.");
        if (source.Length < byteCount)
            throw new ArgumentException("Source too small.", nameof(source));

        ulong result = 0;
        for (int i = 0; i < byteCount; i++)
        {
            result = (result << 8) | source[i];
        }
        return result;
    }
}

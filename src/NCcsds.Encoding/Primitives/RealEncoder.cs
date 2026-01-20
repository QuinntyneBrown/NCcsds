using System.Buffers.Binary;
using NCcsds.Core.Buffers;

namespace NCcsds.Encoding.Primitives;

/// <summary>
/// Encoder/decoder for IEEE 754 floating-point values.
/// </summary>
public static class RealEncoder
{
    /// <summary>
    /// Encodes a single-precision float (32-bit) in big-endian format.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The destination span (4 bytes).</param>
    public static void EncodeSingle(float value, Span<byte> destination)
    {
        if (destination.Length < 4)
            throw new ArgumentException("Destination must be at least 4 bytes.", nameof(destination));

        uint bits = BitConverter.SingleToUInt32Bits(value);
        BinaryPrimitives.WriteUInt32BigEndian(destination, bits);
    }

    /// <summary>
    /// Decodes a single-precision float (32-bit) from big-endian format.
    /// </summary>
    /// <param name="source">The source span (4 bytes).</param>
    /// <returns>The decoded value.</returns>
    public static float DecodeSingle(ReadOnlySpan<byte> source)
    {
        if (source.Length < 4)
            throw new ArgumentException("Source must be at least 4 bytes.", nameof(source));

        uint bits = BinaryPrimitives.ReadUInt32BigEndian(source);
        return BitConverter.UInt32BitsToSingle(bits);
    }

    /// <summary>
    /// Encodes a double-precision float (64-bit) in big-endian format.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The destination span (8 bytes).</param>
    public static void EncodeDouble(double value, Span<byte> destination)
    {
        if (destination.Length < 8)
            throw new ArgumentException("Destination must be at least 8 bytes.", nameof(destination));

        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        BinaryPrimitives.WriteUInt64BigEndian(destination, bits);
    }

    /// <summary>
    /// Decodes a double-precision float (64-bit) from big-endian format.
    /// </summary>
    /// <param name="source">The source span (8 bytes).</param>
    /// <returns>The decoded value.</returns>
    public static double DecodeDouble(ReadOnlySpan<byte> source)
    {
        if (source.Length < 8)
            throw new ArgumentException("Source must be at least 8 bytes.", nameof(source));

        ulong bits = BinaryPrimitives.ReadUInt64BigEndian(source);
        return BitConverter.UInt64BitsToDouble(bits);
    }

    /// <summary>
    /// Encodes a single-precision float using a BitWriter.
    /// </summary>
    public static void EncodeSingle(ref BitWriter writer, float value)
    {
        uint bits = BitConverter.SingleToUInt32Bits(value);
        writer.WriteUInt32(bits);
    }

    /// <summary>
    /// Decodes a single-precision float using a BitReader.
    /// </summary>
    public static float DecodeSingle(ref BitReader reader)
    {
        uint bits = reader.ReadUInt32();
        return BitConverter.UInt32BitsToSingle(bits);
    }

    /// <summary>
    /// Encodes a double-precision float using a BitWriter.
    /// </summary>
    public static void EncodeDouble(ref BitWriter writer, double value)
    {
        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        writer.WriteBits((uint)(bits >> 32), 32);
        writer.WriteBits((uint)(bits & 0xFFFFFFFF), 32);
    }

    /// <summary>
    /// Decodes a double-precision float using a BitReader.
    /// </summary>
    public static double DecodeDouble(ref BitReader reader)
    {
        ulong high = reader.ReadUInt32();
        ulong low = reader.ReadUInt32();
        ulong bits = (high << 32) | low;
        return BitConverter.UInt64BitsToDouble(bits);
    }
}

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace NCcsds.Core.Extensions;

/// <summary>
/// Extension methods for binary operations.
/// </summary>
public static class BinaryExtensions
{
    /// <summary>
    /// Reads a big-endian 16-bit unsigned integer from a span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUInt16BigEndian(this ReadOnlySpan<byte> span) =>
        BinaryPrimitives.ReadUInt16BigEndian(span);

    /// <summary>
    /// Reads a big-endian 32-bit unsigned integer from a span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32BigEndian(this ReadOnlySpan<byte> span) =>
        BinaryPrimitives.ReadUInt32BigEndian(span);

    /// <summary>
    /// Reads a big-endian 64-bit unsigned integer from a span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadUInt64BigEndian(this ReadOnlySpan<byte> span) =>
        BinaryPrimitives.ReadUInt64BigEndian(span);

    /// <summary>
    /// Writes a big-endian 16-bit unsigned integer to a span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt16BigEndian(this Span<byte> span, ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(span, value);

    /// <summary>
    /// Writes a big-endian 32-bit unsigned integer to a span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt32BigEndian(this Span<byte> span, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(span, value);

    /// <summary>
    /// Writes a big-endian 64-bit unsigned integer to a span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt64BigEndian(this Span<byte> span, ulong value) =>
        BinaryPrimitives.WriteUInt64BigEndian(span, value);

    /// <summary>
    /// Reads a 24-bit big-endian unsigned integer from a span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt24BigEndian(this ReadOnlySpan<byte> span) =>
        (uint)((span[0] << 16) | (span[1] << 8) | span[2]);

    /// <summary>
    /// Writes a 24-bit big-endian unsigned integer to a span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt24BigEndian(this Span<byte> span, uint value)
    {
        span[0] = (byte)(value >> 16);
        span[1] = (byte)(value >> 8);
        span[2] = (byte)value;
    }

    /// <summary>
    /// Extracts bits from a value.
    /// </summary>
    /// <param name="value">The value to extract from.</param>
    /// <param name="startBit">The starting bit position (0 = LSB).</param>
    /// <param name="bitCount">The number of bits to extract.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ExtractBits(this uint value, int startBit, int bitCount)
    {
        uint mask = (1u << bitCount) - 1;
        return (value >> startBit) & mask;
    }

    /// <summary>
    /// Extracts bits from a value.
    /// </summary>
    /// <param name="value">The value to extract from.</param>
    /// <param name="startBit">The starting bit position (0 = LSB).</param>
    /// <param name="bitCount">The number of bits to extract.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ExtractBits(this ushort value, int startBit, int bitCount)
    {
        ushort mask = (ushort)((1 << bitCount) - 1);
        return (ushort)((value >> startBit) & mask);
    }

    /// <summary>
    /// Inserts bits into a value.
    /// </summary>
    /// <param name="value">The value to modify.</param>
    /// <param name="bits">The bits to insert.</param>
    /// <param name="startBit">The starting bit position (0 = LSB).</param>
    /// <param name="bitCount">The number of bits to insert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint InsertBits(this uint value, uint bits, int startBit, int bitCount)
    {
        uint mask = (1u << bitCount) - 1;
        bits &= mask;
        value &= ~(mask << startBit);
        return value | (bits << startBit);
    }

    /// <summary>
    /// Converts a hex string to a byte array.
    /// </summary>
    /// <param name="hex">The hex string (with or without spaces/dashes).</param>
    /// <returns>The byte array.</returns>
    public static byte[] HexToBytes(this string hex)
    {
        // Remove whitespace and common separators
        hex = hex.Replace(" ", "").Replace("-", "").Replace(":", "");

        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have even length.", nameof(hex));

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    /// <summary>
    /// Converts a byte array to a hex string.
    /// </summary>
    /// <param name="bytes">The byte array.</param>
    /// <param name="separator">Optional separator between bytes.</param>
    /// <returns>The hex string.</returns>
    public static string ToHexString(this ReadOnlySpan<byte> bytes, string separator = "")
    {
        if (bytes.IsEmpty)
            return string.Empty;

        if (string.IsNullOrEmpty(separator))
            return Convert.ToHexString(bytes);

        return string.Join(separator, bytes.ToArray().Select(b => b.ToString("X2")));
    }

    /// <summary>
    /// Converts a byte array to a hex string.
    /// </summary>
    public static string ToHexString(this byte[] bytes, string separator = "") =>
        ToHexString((ReadOnlySpan<byte>)bytes, separator);
}

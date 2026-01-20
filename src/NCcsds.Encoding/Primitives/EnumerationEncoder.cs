using NCcsds.Core.Buffers;

namespace NCcsds.Encoding.Primitives;

/// <summary>
/// Encoder/decoder for enumeration values.
/// </summary>
public static class EnumerationEncoder
{
    /// <summary>
    /// Encodes an enumeration value with the specified bit width.
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="writer">The bit writer.</param>
    /// <param name="value">The enumeration value.</param>
    /// <param name="bitWidth">The bit width.</param>
    public static void Encode<T>(ref BitWriter writer, T value, int bitWidth) where T : Enum
    {
        ulong numericValue = Convert.ToUInt64(value);
        IntegerEncoder.EncodeUnsigned(ref writer, numericValue, bitWidth);
    }

    /// <summary>
    /// Decodes an enumeration value with the specified bit width.
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="reader">The bit reader.</param>
    /// <param name="bitWidth">The bit width.</param>
    /// <returns>The enumeration value.</returns>
    public static T Decode<T>(ref BitReader reader, int bitWidth) where T : Enum
    {
        ulong numericValue = IntegerEncoder.DecodeUnsigned(ref reader, bitWidth);
        return (T)Enum.ToObject(typeof(T), numericValue);
    }

    /// <summary>
    /// Encodes an enumeration value to bytes.
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="value">The enumeration value.</param>
    /// <param name="byteCount">The number of bytes.</param>
    /// <param name="destination">The destination span.</param>
    public static void EncodeToBytes<T>(T value, int byteCount, Span<byte> destination) where T : Enum
    {
        ulong numericValue = Convert.ToUInt64(value);
        IntegerEncoder.EncodeToBytes(numericValue, byteCount, destination);
    }

    /// <summary>
    /// Decodes an enumeration value from bytes.
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="source">The source span.</param>
    /// <param name="byteCount">The number of bytes.</param>
    /// <returns>The enumeration value.</returns>
    public static T DecodeFromBytes<T>(ReadOnlySpan<byte> source, int byteCount) where T : Enum
    {
        ulong numericValue = IntegerEncoder.DecodeFromBytes(source, byteCount);
        return (T)Enum.ToObject(typeof(T), numericValue);
    }

    /// <summary>
    /// Tries to decode an enumeration value, validating it's defined.
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="reader">The bit reader.</param>
    /// <param name="bitWidth">The bit width.</param>
    /// <param name="value">The decoded value if valid.</param>
    /// <returns>True if the value is a defined enumeration member.</returns>
    public static bool TryDecode<T>(ref BitReader reader, int bitWidth, out T value) where T : struct, Enum
    {
        ulong numericValue = IntegerEncoder.DecodeUnsigned(ref reader, bitWidth);
        value = (T)Enum.ToObject(typeof(T), numericValue);
        return Enum.IsDefined(typeof(T), value);
    }
}

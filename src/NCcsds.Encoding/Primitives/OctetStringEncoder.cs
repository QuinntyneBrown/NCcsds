namespace NCcsds.Encoding.Primitives;

/// <summary>
/// Encoder/decoder for octet (byte) strings.
/// </summary>
public static class OctetStringEncoder
{
    /// <summary>
    /// Encodes a fixed-length octet string.
    /// </summary>
    /// <param name="value">The bytes to encode.</param>
    /// <param name="fixedLength">The fixed length.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="padByte">The padding byte (default: 0).</param>
    public static void EncodeFixed(ReadOnlySpan<byte> value, int fixedLength, Span<byte> destination, byte padByte = 0)
    {
        if (destination.Length < fixedLength)
            throw new ArgumentException("Destination too small.", nameof(destination));

        destination[..fixedLength].Fill(padByte);
        value[..Math.Min(value.Length, fixedLength)].CopyTo(destination);
    }

    /// <summary>
    /// Decodes a fixed-length octet string.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="fixedLength">The fixed length.</param>
    /// <returns>The decoded bytes.</returns>
    public static byte[] DecodeFixed(ReadOnlySpan<byte> source, int fixedLength)
    {
        if (source.Length < fixedLength)
            throw new ArgumentException("Source too small.", nameof(source));

        return source[..fixedLength].ToArray();
    }

    /// <summary>
    /// Encodes a variable-length octet string with a length prefix.
    /// </summary>
    /// <param name="value">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="lengthBytes">Number of bytes for the length prefix (1, 2, or 4).</param>
    /// <returns>Total bytes written.</returns>
    public static int EncodeVariable(ReadOnlySpan<byte> value, Span<byte> destination, int lengthBytes = 2)
    {
        int totalBytes = lengthBytes + value.Length;

        if (destination.Length < totalBytes)
            throw new ArgumentException("Destination too small.", nameof(destination));

        // Validate length fits in prefix
        ulong maxLength = lengthBytes switch
        {
            1 => 255,
            2 => 65535,
            4 => uint.MaxValue,
            _ => throw new ArgumentException("Length bytes must be 1, 2, or 4.", nameof(lengthBytes))
        };

        if ((ulong)value.Length > maxLength)
            throw new ArgumentException($"Value too long for {lengthBytes}-byte length prefix.", nameof(value));

        // Write length prefix
        IntegerEncoder.EncodeToBytes((ulong)value.Length, lengthBytes, destination);
        value.CopyTo(destination[lengthBytes..]);

        return totalBytes;
    }

    /// <summary>
    /// Decodes a variable-length octet string with a length prefix.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="lengthBytes">Number of bytes for the length prefix.</param>
    /// <param name="bytesRead">Total bytes read.</param>
    /// <returns>The decoded bytes.</returns>
    public static byte[] DecodeVariable(ReadOnlySpan<byte> source, int lengthBytes, out int bytesRead)
    {
        if (source.Length < lengthBytes)
            throw new ArgumentException("Source too small for length prefix.", nameof(source));

        int dataLength = (int)IntegerEncoder.DecodeFromBytes(source, lengthBytes);
        bytesRead = lengthBytes + dataLength;

        if (source.Length < bytesRead)
            throw new ArgumentException("Source too small for data.", nameof(source));

        return source.Slice(lengthBytes, dataLength).ToArray();
    }

    /// <summary>
    /// Copies a variable-length octet string to a destination span.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="lengthBytes">Number of bytes for the length prefix.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="bytesRead">Total bytes read from source.</param>
    /// <returns>Number of data bytes copied.</returns>
    public static int DecodeVariableTo(ReadOnlySpan<byte> source, int lengthBytes, Span<byte> destination, out int bytesRead)
    {
        if (source.Length < lengthBytes)
            throw new ArgumentException("Source too small for length prefix.", nameof(source));

        int dataLength = (int)IntegerEncoder.DecodeFromBytes(source, lengthBytes);
        bytesRead = lengthBytes + dataLength;

        if (source.Length < bytesRead)
            throw new ArgumentException("Source too small for data.", nameof(source));
        if (destination.Length < dataLength)
            throw new ArgumentException("Destination too small for data.", nameof(destination));

        source.Slice(lengthBytes, dataLength).CopyTo(destination);
        return dataLength;
    }
}

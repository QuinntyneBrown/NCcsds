using TextEncoding = System.Text.Encoding;

namespace NCcsds.Encoding.Primitives;

/// <summary>
/// Encoder/decoder for character strings.
/// </summary>
public static class StringEncoder
{
    /// <summary>
    /// Encodes a fixed-length ASCII string.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <param name="fixedLength">The fixed length in bytes.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="padChar">The padding character (default: null/0).</param>
    public static void EncodeFixedAscii(string value, int fixedLength, Span<byte> destination, byte padChar = 0)
    {
        if (destination.Length < fixedLength)
            throw new ArgumentException("Destination too small.", nameof(destination));

        destination[..fixedLength].Fill(padChar);

        int bytesToWrite = Math.Min(value.Length, fixedLength);
        TextEncoding.ASCII.GetBytes(value.AsSpan(0, bytesToWrite), destination);
    }

    /// <summary>
    /// Decodes a fixed-length ASCII string.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="fixedLength">The fixed length in bytes.</param>
    /// <param name="trimNulls">Whether to trim null characters.</param>
    /// <returns>The decoded string.</returns>
    public static string DecodeFixedAscii(ReadOnlySpan<byte> source, int fixedLength, bool trimNulls = true)
    {
        if (source.Length < fixedLength)
            throw new ArgumentException("Source too small.", nameof(source));

        var bytes = source[..fixedLength];
        var str = TextEncoding.ASCII.GetString(bytes);

        if (trimNulls)
            str = str.TrimEnd('\0');

        return str;
    }

    /// <summary>
    /// Encodes a variable-length string with a length prefix.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="lengthBytes">Number of bytes for the length prefix (1, 2, or 4).</param>
    /// <returns>Total bytes written.</returns>
    public static int EncodeVariableAscii(string value, Span<byte> destination, int lengthBytes = 2)
    {
        int stringBytes = TextEncoding.ASCII.GetByteCount(value);
        int totalBytes = lengthBytes + stringBytes;

        if (destination.Length < totalBytes)
            throw new ArgumentException("Destination too small.", nameof(destination));

        // Write length prefix
        switch (lengthBytes)
        {
            case 1:
                if (stringBytes > 255)
                    throw new ArgumentException("String too long for 1-byte length prefix.", nameof(value));
                destination[0] = (byte)stringBytes;
                break;
            case 2:
                if (stringBytes > 65535)
                    throw new ArgumentException("String too long for 2-byte length prefix.", nameof(value));
                destination[0] = (byte)(stringBytes >> 8);
                destination[1] = (byte)(stringBytes & 0xFF);
                break;
            case 4:
                destination[0] = (byte)(stringBytes >> 24);
                destination[1] = (byte)(stringBytes >> 16);
                destination[2] = (byte)(stringBytes >> 8);
                destination[3] = (byte)(stringBytes & 0xFF);
                break;
            default:
                throw new ArgumentException("Length bytes must be 1, 2, or 4.", nameof(lengthBytes));
        }

        TextEncoding.ASCII.GetBytes(value, destination[lengthBytes..]);
        return totalBytes;
    }

    /// <summary>
    /// Decodes a variable-length string with a length prefix.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="lengthBytes">Number of bytes for the length prefix.</param>
    /// <param name="bytesRead">Total bytes read.</param>
    /// <returns>The decoded string.</returns>
    public static string DecodeVariableAscii(ReadOnlySpan<byte> source, int lengthBytes, out int bytesRead)
    {
        if (source.Length < lengthBytes)
            throw new ArgumentException("Source too small for length prefix.", nameof(source));

        int stringLength = lengthBytes switch
        {
            1 => source[0],
            2 => (source[0] << 8) | source[1],
            4 => (source[0] << 24) | (source[1] << 16) | (source[2] << 8) | source[3],
            _ => throw new ArgumentException("Length bytes must be 1, 2, or 4.", nameof(lengthBytes))
        };

        bytesRead = lengthBytes + stringLength;

        if (source.Length < bytesRead)
            throw new ArgumentException("Source too small for string data.", nameof(source));

        return TextEncoding.ASCII.GetString(source.Slice(lengthBytes, stringLength));
    }

    /// <summary>
    /// Encodes a UTF-8 string with a length prefix.
    /// </summary>
    public static int EncodeVariableUtf8(string value, Span<byte> destination, int lengthBytes = 2)
    {
        int stringBytes = TextEncoding.UTF8.GetByteCount(value);
        int totalBytes = lengthBytes + stringBytes;

        if (destination.Length < totalBytes)
            throw new ArgumentException("Destination too small.", nameof(destination));

        // Write length prefix (big-endian)
        IntegerEncoder.EncodeToBytes((ulong)stringBytes, lengthBytes, destination);
        TextEncoding.UTF8.GetBytes(value, destination[lengthBytes..]);

        return totalBytes;
    }

    /// <summary>
    /// Decodes a UTF-8 string with a length prefix.
    /// </summary>
    public static string DecodeVariableUtf8(ReadOnlySpan<byte> source, int lengthBytes, out int bytesRead)
    {
        if (source.Length < lengthBytes)
            throw new ArgumentException("Source too small for length prefix.", nameof(source));

        int stringLength = (int)IntegerEncoder.DecodeFromBytes(source, lengthBytes);
        bytesRead = lengthBytes + stringLength;

        if (source.Length < bytesRead)
            throw new ArgumentException("Source too small for string data.", nameof(source));

        return TextEncoding.UTF8.GetString(source.Slice(lengthBytes, stringLength));
    }
}

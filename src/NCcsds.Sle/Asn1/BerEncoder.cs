namespace NCcsds.Sle.Asn1;

/// <summary>
/// ASN.1 BER (Basic Encoding Rules) encoder.
/// </summary>
public static class BerEncoder
{
    /// <summary>
    /// ASN.1 universal tag types.
    /// </summary>
    public enum Tag : byte
    {
        Boolean = 0x01,
        Integer = 0x02,
        BitString = 0x03,
        OctetString = 0x04,
        Null = 0x05,
        ObjectIdentifier = 0x06,
        Utf8String = 0x0C,
        Sequence = 0x30,
        Set = 0x31,
        PrintableString = 0x13,
        Ia5String = 0x16,
        UtcTime = 0x17,
        GeneralizedTime = 0x18,
        VisibleString = 0x1A
    }

    /// <summary>
    /// Encodes a boolean value.
    /// </summary>
    public static byte[] EncodeBoolean(bool value)
    {
        return [0x01, 0x01, value ? (byte)0xFF : (byte)0x00];
    }

    /// <summary>
    /// Encodes an integer value.
    /// </summary>
    public static byte[] EncodeInteger(long value)
    {
        var bytes = new List<byte>();

        if (value == 0)
        {
            return [0x02, 0x01, 0x00];
        }

        var isNegative = value < 0;
        var absValue = isNegative ? -value : value;

        while (absValue > 0)
        {
            bytes.Insert(0, (byte)(absValue & 0xFF));
            absValue >>= 8;
        }

        // Handle sign bit
        if (isNegative)
        {
            // Two's complement
            for (int i = 0; i < bytes.Count; i++)
                bytes[i] = (byte)~bytes[i];

            // Add 1 for two's complement
            for (int i = bytes.Count - 1; i >= 0; i--)
            {
                bytes[i]++;
                if (bytes[i] != 0) break;
            }
        }
        else if ((bytes[0] & 0x80) != 0)
        {
            bytes.Insert(0, 0x00); // Positive but high bit set
        }

        var result = new byte[2 + bytes.Count];
        result[0] = 0x02; // Integer tag
        result[1] = (byte)bytes.Count;
        bytes.CopyTo(result, 2);

        return result;
    }

    /// <summary>
    /// Encodes an octet string.
    /// </summary>
    public static byte[] EncodeOctetString(byte[] data)
    {
        return EncodeTagLengthValue(Tag.OctetString, data);
    }

    /// <summary>
    /// Encodes a visible string.
    /// </summary>
    public static byte[] EncodeVisibleString(string value)
    {
        var data = System.Text.Encoding.ASCII.GetBytes(value);
        return EncodeTagLengthValue(Tag.VisibleString, data);
    }

    /// <summary>
    /// Encodes a sequence.
    /// </summary>
    public static byte[] EncodeSequence(params byte[][] elements)
    {
        var totalLength = elements.Sum(e => e.Length);
        var data = new byte[totalLength];
        var offset = 0;
        foreach (var element in elements)
        {
            element.CopyTo(data, offset);
            offset += element.Length;
        }
        return EncodeTagLengthValue(Tag.Sequence, data);
    }

    /// <summary>
    /// Encodes a context-specific tagged value.
    /// </summary>
    public static byte[] EncodeContextSpecific(int tag, byte[] value, bool constructed = false)
    {
        var tagByte = (byte)(0x80 | (constructed ? 0x20 : 0x00) | (tag & 0x1F));
        return EncodeTagLengthValueRaw(tagByte, value);
    }

    /// <summary>
    /// Encodes tag, length, and value.
    /// </summary>
    public static byte[] EncodeTagLengthValue(Tag tag, byte[] value)
    {
        return EncodeTagLengthValueRaw((byte)tag, value);
    }

    private static byte[] EncodeTagLengthValueRaw(byte tag, byte[] value)
    {
        var lengthBytes = EncodeLength(value.Length);
        var result = new byte[1 + lengthBytes.Length + value.Length];
        result[0] = tag;
        lengthBytes.CopyTo(result, 1);
        value.CopyTo(result, 1 + lengthBytes.Length);
        return result;
    }

    /// <summary>
    /// Encodes length in BER format.
    /// </summary>
    public static byte[] EncodeLength(int length)
    {
        if (length < 128)
        {
            return [(byte)length];
        }

        var bytes = new List<byte>();
        var temp = length;
        while (temp > 0)
        {
            bytes.Insert(0, (byte)(temp & 0xFF));
            temp >>= 8;
        }

        var result = new byte[1 + bytes.Count];
        result[0] = (byte)(0x80 | bytes.Count);
        bytes.CopyTo(result, 1);
        return result;
    }

    /// <summary>
    /// Encodes a CCSDS time (CDS format) for SLE.
    /// </summary>
    public static byte[] EncodeTime(DateTime time)
    {
        // CCSDS Day Segmented Time Code
        var epoch = new DateTime(1958, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var days = (ushort)(time - epoch).TotalDays;
        var ms = (uint)((time - epoch.AddDays(days)).TotalMilliseconds);
        var us = (ushort)((time - epoch.AddDays(days).AddMilliseconds(ms)).TotalMicroseconds);

        var data = new byte[8];
        data[0] = (byte)(days >> 8);
        data[1] = (byte)days;
        data[2] = (byte)(ms >> 24);
        data[3] = (byte)(ms >> 16);
        data[4] = (byte)(ms >> 8);
        data[5] = (byte)ms;
        data[6] = (byte)(us >> 8);
        data[7] = (byte)us;

        return EncodeOctetString(data);
    }
}

/// <summary>
/// ASN.1 BER (Basic Encoding Rules) decoder.
/// </summary>
public static class BerDecoder
{
    /// <summary>
    /// Decodes a BER-encoded boolean.
    /// </summary>
    public static bool DecodeBoolean(ReadOnlySpan<byte> data, out int bytesConsumed)
    {
        if (data.Length < 3 || data[0] != 0x01)
            throw new FormatException("Invalid boolean encoding");

        bytesConsumed = 3;
        return data[2] != 0;
    }

    /// <summary>
    /// Decodes a BER-encoded integer.
    /// </summary>
    public static long DecodeInteger(ReadOnlySpan<byte> data, out int bytesConsumed)
    {
        if (data.Length < 2 || data[0] != 0x02)
            throw new FormatException("Invalid integer encoding");

        var length = DecodeLength(data[1..], out var lengthBytes);
        var valueStart = 1 + lengthBytes;

        if (data.Length < valueStart + length)
            throw new FormatException("Insufficient data for integer");

        long value = 0;
        var isNegative = (data[valueStart] & 0x80) != 0;

        for (int i = 0; i < length; i++)
        {
            value = (value << 8) | data[valueStart + i];
        }

        if (isNegative)
        {
            // Sign extend
            for (int i = length; i < 8; i++)
                value |= (long)0xFF << (i * 8);
        }

        bytesConsumed = valueStart + length;
        return value;
    }

    /// <summary>
    /// Decodes a BER-encoded octet string.
    /// </summary>
    public static byte[] DecodeOctetString(ReadOnlySpan<byte> data, out int bytesConsumed)
    {
        if (data.Length < 2 || data[0] != 0x04)
            throw new FormatException("Invalid octet string encoding");

        var length = DecodeLength(data[1..], out var lengthBytes);
        var valueStart = 1 + lengthBytes;

        if (data.Length < valueStart + length)
            throw new FormatException("Insufficient data for octet string");

        bytesConsumed = valueStart + length;
        return data.Slice(valueStart, length).ToArray();
    }

    /// <summary>
    /// Decodes a BER-encoded visible string.
    /// </summary>
    public static string DecodeVisibleString(ReadOnlySpan<byte> data, out int bytesConsumed)
    {
        if (data.Length < 2 || data[0] != 0x1A)
            throw new FormatException("Invalid visible string encoding");

        var length = DecodeLength(data[1..], out var lengthBytes);
        var valueStart = 1 + lengthBytes;

        if (data.Length < valueStart + length)
            throw new FormatException("Insufficient data for visible string");

        bytesConsumed = valueStart + length;
        return System.Text.Encoding.ASCII.GetString(data.Slice(valueStart, length));
    }

    /// <summary>
    /// Decodes BER length.
    /// </summary>
    public static int DecodeLength(ReadOnlySpan<byte> data, out int bytesConsumed)
    {
        if (data.Length == 0)
            throw new FormatException("Empty length encoding");

        if ((data[0] & 0x80) == 0)
        {
            bytesConsumed = 1;
            return data[0];
        }

        var numBytes = data[0] & 0x7F;
        if (data.Length < 1 + numBytes)
            throw new FormatException("Insufficient data for length");

        int length = 0;
        for (int i = 0; i < numBytes; i++)
        {
            length = (length << 8) | data[1 + i];
        }

        bytesConsumed = 1 + numBytes;
        return length;
    }

    /// <summary>
    /// Decodes a sequence, returning the content and tag/length consumed.
    /// </summary>
    public static ReadOnlySpan<byte> DecodeSequence(ReadOnlySpan<byte> data, out int bytesConsumed)
    {
        if (data.Length < 2 || data[0] != 0x30)
            throw new FormatException("Invalid sequence encoding");

        var length = DecodeLength(data[1..], out var lengthBytes);
        var valueStart = 1 + lengthBytes;

        if (data.Length < valueStart + length)
            throw new FormatException("Insufficient data for sequence");

        bytesConsumed = valueStart + length;
        return data.Slice(valueStart, length);
    }

    /// <summary>
    /// Gets the tag from BER data.
    /// </summary>
    public static byte GetTag(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            throw new FormatException("Empty data");
        return data[0];
    }

    /// <summary>
    /// Checks if tag is context-specific.
    /// </summary>
    public static bool IsContextSpecific(byte tag, out int tagNumber, out bool isConstructed)
    {
        isConstructed = (tag & 0x20) != 0;
        tagNumber = tag & 0x1F;
        return (tag & 0xC0) == 0x80;
    }
}

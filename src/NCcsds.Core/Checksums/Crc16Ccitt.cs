namespace NCcsds.Core.Checksums;

/// <summary>
/// CRC-16-CCITT calculator used in CCSDS frames (polynomial 0x1021).
/// </summary>
public static class Crc16Ccitt
{
    /// <summary>
    /// The polynomial used for CRC-16-CCITT (x^16 + x^12 + x^5 + 1).
    /// </summary>
    public const ushort Polynomial = 0x1021;

    /// <summary>
    /// Initial value for CCSDS CRC-16 (all ones).
    /// </summary>
    public const ushort InitialValue = 0xFFFF;

    private static readonly ushort[] Table = GenerateTable();

    private static ushort[] GenerateTable()
    {
        var table = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            ushort crc = (ushort)(i << 8);
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ Polynomial);
                else
                    crc <<= 1;
            }
            table[i] = crc;
        }
        return table;
    }

    /// <summary>
    /// Computes the CRC-16-CCITT for the given data.
    /// </summary>
    /// <param name="data">The data to compute CRC for.</param>
    /// <returns>The computed CRC value.</returns>
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        return Compute(data, InitialValue);
    }

    /// <summary>
    /// Computes the CRC-16-CCITT for the given data with a custom initial value.
    /// </summary>
    /// <param name="data">The data to compute CRC for.</param>
    /// <param name="initialValue">The initial CRC value.</param>
    /// <returns>The computed CRC value.</returns>
    public static ushort Compute(ReadOnlySpan<byte> data, ushort initialValue)
    {
        ushort crc = initialValue;
        foreach (byte b in data)
        {
            crc = (ushort)((crc << 8) ^ Table[(crc >> 8) ^ b]);
        }
        return crc;
    }

    /// <summary>
    /// Validates that the CRC of the data (including the CRC bytes) is valid.
    /// For CCSDS, a valid frame should result in a residual of 0x0000.
    /// </summary>
    /// <param name="dataWithCrc">The data including the CRC bytes at the end.</param>
    /// <returns>True if the CRC is valid.</returns>
    public static bool Validate(ReadOnlySpan<byte> dataWithCrc)
    {
        return Compute(dataWithCrc) == 0;
    }

    /// <summary>
    /// Appends the CRC to a buffer containing data.
    /// </summary>
    /// <param name="dataBuffer">Buffer with space for CRC at the end.</param>
    /// <param name="dataLength">Length of the data (excluding CRC space).</param>
    public static void Append(Span<byte> dataBuffer, int dataLength)
    {
        var crc = Compute(dataBuffer[..dataLength]);
        dataBuffer[dataLength] = (byte)(crc >> 8);
        dataBuffer[dataLength + 1] = (byte)(crc & 0xFF);
    }
}

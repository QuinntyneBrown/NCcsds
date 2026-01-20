namespace NCcsds.Core.Checksums;

/// <summary>
/// CRC-32 calculator (ISO 3309 / IEEE 802.3 polynomial).
/// </summary>
public static class Crc32
{
    /// <summary>
    /// The polynomial used for CRC-32.
    /// </summary>
    public const uint Polynomial = 0xEDB88320;

    /// <summary>
    /// Initial value for CRC-32 (all ones).
    /// </summary>
    public const uint InitialValue = 0xFFFFFFFF;

    private static readonly uint[] Table = GenerateTable();

    private static uint[] GenerateTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ Polynomial;
                else
                    crc >>= 1;
            }
            table[i] = crc;
        }
        return table;
    }

    /// <summary>
    /// Computes the CRC-32 for the given data.
    /// </summary>
    /// <param name="data">The data to compute CRC for.</param>
    /// <returns>The computed CRC value.</returns>
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        return Compute(data, InitialValue);
    }

    /// <summary>
    /// Computes the CRC-32 for the given data with a custom initial value.
    /// </summary>
    /// <param name="data">The data to compute CRC for.</param>
    /// <param name="initialValue">The initial CRC value.</param>
    /// <returns>The computed CRC value.</returns>
    public static uint Compute(ReadOnlySpan<byte> data, uint initialValue)
    {
        uint crc = initialValue;
        foreach (byte b in data)
        {
            crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
        }
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// Continues CRC computation with additional data.
    /// </summary>
    /// <param name="data">Additional data.</param>
    /// <param name="previousCrc">Previous CRC value (before final XOR).</param>
    /// <returns>The updated CRC value.</returns>
    public static uint Continue(ReadOnlySpan<byte> data, uint previousCrc)
    {
        // Undo the final XOR from the previous computation
        uint crc = previousCrc ^ 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
        }
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// Validates that the CRC of the data (including the CRC bytes) is valid.
    /// </summary>
    /// <param name="dataWithCrc">The data including the CRC bytes at the end.</param>
    /// <returns>True if the CRC is valid.</returns>
    public static bool Validate(ReadOnlySpan<byte> dataWithCrc)
    {
        // Computing CRC over data + CRC should give a known constant
        return Compute(dataWithCrc) == 0x2144DF1C;
    }
}

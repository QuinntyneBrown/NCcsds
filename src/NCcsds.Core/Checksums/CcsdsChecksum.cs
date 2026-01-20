namespace NCcsds.Core.Checksums;

/// <summary>
/// CCSDS modular checksum (8-bit sum complement) used in CFDP and other protocols.
/// </summary>
public static class CcsdsChecksum
{
    /// <summary>
    /// Computes the CCSDS modular checksum for the given data.
    /// </summary>
    /// <param name="data">The data to compute checksum for.</param>
    /// <returns>The computed checksum value.</returns>
    public static byte Compute(ReadOnlySpan<byte> data)
    {
        int sum = 0;
        foreach (byte b in data)
        {
            sum += b;
        }
        return (byte)(~sum + 1);
    }

    /// <summary>
    /// Validates that the checksum of the data (including the checksum byte) is valid.
    /// </summary>
    /// <param name="dataWithChecksum">The data including the checksum byte at the end.</param>
    /// <returns>True if the checksum is valid.</returns>
    public static bool Validate(ReadOnlySpan<byte> dataWithChecksum)
    {
        int sum = 0;
        foreach (byte b in dataWithChecksum)
        {
            sum += b;
        }
        return (byte)sum == 0;
    }

    /// <summary>
    /// Computes the 32-bit CCSDS checksum used in CFDP.
    /// </summary>
    /// <param name="data">The data to compute checksum for.</param>
    /// <returns>The computed 32-bit checksum.</returns>
    public static uint Compute32(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        int i = 0;

        // Process 4 bytes at a time
        while (i + 4 <= data.Length)
        {
            sum += (uint)((data[i] << 24) | (data[i + 1] << 16) | (data[i + 2] << 8) | data[i + 3]);
            i += 4;
        }

        // Handle remaining bytes
        if (i < data.Length)
        {
            uint partial = 0;
            int shift = 24;
            while (i < data.Length)
            {
                partial |= (uint)(data[i] << shift);
                shift -= 8;
                i++;
            }
            sum += partial;
        }

        return sum;
    }
}

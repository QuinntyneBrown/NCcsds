namespace NCcsds.Core.Buffers;

/// <summary>
/// Provides bit-level read operations on a byte span.
/// </summary>
public ref struct BitReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _bitPosition;

    /// <summary>
    /// Creates a new bit reader over the specified data.
    /// </summary>
    /// <param name="data">The data to read from.</param>
    public BitReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _bitPosition = 0;
    }

    /// <summary>
    /// Gets the current bit position.
    /// </summary>
    public int BitPosition => _bitPosition;

    /// <summary>
    /// Gets the current byte position.
    /// </summary>
    public int BytePosition => _bitPosition / 8;

    /// <summary>
    /// Gets the total number of bits available.
    /// </summary>
    public int TotalBits => _data.Length * 8;

    /// <summary>
    /// Gets the number of bits remaining.
    /// </summary>
    public int RemainingBits => TotalBits - _bitPosition;

    /// <summary>
    /// Reads the specified number of bits as an unsigned integer.
    /// </summary>
    /// <param name="bitCount">Number of bits to read (1-32).</param>
    /// <returns>The unsigned integer value.</returns>
    public uint ReadBits(int bitCount)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount), "Bit count must be between 1 and 32.");
        if (_bitPosition + bitCount > TotalBits)
            throw new InvalidOperationException("Not enough bits remaining.");

        uint result = 0;
        int bitsRemaining = bitCount;

        while (bitsRemaining > 0)
        {
            int byteIndex = _bitPosition / 8;
            int bitOffset = _bitPosition % 8;
            int bitsAvailableInByte = 8 - bitOffset;
            int bitsToRead = Math.Min(bitsRemaining, bitsAvailableInByte);

            int mask = (1 << bitsToRead) - 1;
            int shift = bitsAvailableInByte - bitsToRead;
            uint bits = (uint)((_data[byteIndex] >> shift) & mask);

            result = (result << bitsToRead) | bits;
            _bitPosition += bitsToRead;
            bitsRemaining -= bitsToRead;
        }

        return result;
    }

    /// <summary>
    /// Reads a single bit as a boolean.
    /// </summary>
    public bool ReadBit() => ReadBits(1) != 0;

    /// <summary>
    /// Reads 8 bits as a byte.
    /// </summary>
    public byte ReadByte() => (byte)ReadBits(8);

    /// <summary>
    /// Reads 16 bits as an unsigned short (big-endian).
    /// </summary>
    public ushort ReadUInt16() => (ushort)ReadBits(16);

    /// <summary>
    /// Reads 32 bits as an unsigned integer (big-endian).
    /// </summary>
    public uint ReadUInt32() => ReadBits(32);

    /// <summary>
    /// Reads the specified number of bits as a signed integer.
    /// </summary>
    /// <param name="bitCount">Number of bits to read (1-32).</param>
    /// <returns>The signed integer value.</returns>
    public int ReadSignedBits(int bitCount)
    {
        uint value = ReadBits(bitCount);
        // Sign extend if the high bit is set
        if ((value & (1u << (bitCount - 1))) != 0)
        {
            uint mask = uint.MaxValue << bitCount;
            value |= mask;
        }
        return (int)value;
    }

    /// <summary>
    /// Skips the specified number of bits.
    /// </summary>
    /// <param name="bitCount">Number of bits to skip.</param>
    public void Skip(int bitCount)
    {
        if (_bitPosition + bitCount > TotalBits)
            throw new InvalidOperationException("Not enough bits remaining.");
        _bitPosition += bitCount;
    }

    /// <summary>
    /// Aligns the position to the next byte boundary.
    /// </summary>
    public void AlignToByte()
    {
        int remainder = _bitPosition % 8;
        if (remainder != 0)
            _bitPosition += 8 - remainder;
    }

    /// <summary>
    /// Resets the position to the beginning.
    /// </summary>
    public void Reset() => _bitPosition = 0;

    /// <summary>
    /// Seeks to the specified bit position.
    /// </summary>
    /// <param name="bitPosition">The bit position to seek to.</param>
    public void Seek(int bitPosition)
    {
        if (bitPosition < 0 || bitPosition > TotalBits)
            throw new ArgumentOutOfRangeException(nameof(bitPosition));
        _bitPosition = bitPosition;
    }
}

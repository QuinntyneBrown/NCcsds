namespace NCcsds.Core.Buffers;

/// <summary>
/// Provides bit-level write operations on a byte span.
/// </summary>
public ref struct BitWriter
{
    private readonly Span<byte> _data;
    private int _bitPosition;

    /// <summary>
    /// Creates a new bit writer over the specified data.
    /// </summary>
    /// <param name="data">The data to write to.</param>
    public BitWriter(Span<byte> data)
    {
        _data = data;
        _bitPosition = 0;
        _data.Clear();
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
    /// Gets the number of bytes written (rounded up).
    /// </summary>
    public int BytesWritten => (_bitPosition + 7) / 8;

    /// <summary>
    /// Gets the total number of bits available.
    /// </summary>
    public int TotalBits => _data.Length * 8;

    /// <summary>
    /// Gets the number of bits remaining.
    /// </summary>
    public int RemainingBits => TotalBits - _bitPosition;

    /// <summary>
    /// Writes the specified number of bits from an unsigned integer.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="bitCount">Number of bits to write (1-32).</param>
    public void WriteBits(uint value, int bitCount)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount), "Bit count must be between 1 and 32.");
        if (_bitPosition + bitCount > TotalBits)
            throw new InvalidOperationException("Not enough bits remaining.");

        // Mask to ensure we only use the specified number of bits
        uint mask = bitCount == 32 ? uint.MaxValue : (1u << bitCount) - 1;
        value &= mask;

        int bitsRemaining = bitCount;
        int valueShift = bitCount;

        while (bitsRemaining > 0)
        {
            int byteIndex = _bitPosition / 8;
            int bitOffset = _bitPosition % 8;
            int bitsAvailableInByte = 8 - bitOffset;
            int bitsToWrite = Math.Min(bitsRemaining, bitsAvailableInByte);

            valueShift -= bitsToWrite;
            int shift = bitsAvailableInByte - bitsToWrite;
            byte bits = (byte)((value >> valueShift) << shift);
            byte byteMask = (byte)(((1 << bitsToWrite) - 1) << shift);

            _data[byteIndex] = (byte)((_data[byteIndex] & ~byteMask) | bits);
            _bitPosition += bitsToWrite;
            bitsRemaining -= bitsToWrite;
        }
    }

    /// <summary>
    /// Writes a single bit.
    /// </summary>
    /// <param name="value">The bit value.</param>
    public void WriteBit(bool value) => WriteBits(value ? 1u : 0u, 1);

    /// <summary>
    /// Writes 8 bits from a byte.
    /// </summary>
    /// <param name="value">The byte value.</param>
    public void WriteByte(byte value) => WriteBits(value, 8);

    /// <summary>
    /// Writes 16 bits from an unsigned short (big-endian).
    /// </summary>
    /// <param name="value">The ushort value.</param>
    public void WriteUInt16(ushort value) => WriteBits(value, 16);

    /// <summary>
    /// Writes 32 bits from an unsigned integer (big-endian).
    /// </summary>
    /// <param name="value">The uint value.</param>
    public void WriteUInt32(uint value) => WriteBits(value, 32);

    /// <summary>
    /// Writes the specified number of bits from a signed integer.
    /// </summary>
    /// <param name="value">The signed value to write.</param>
    /// <param name="bitCount">Number of bits to write (1-32).</param>
    public void WriteSignedBits(int value, int bitCount)
    {
        WriteBits((uint)value, bitCount);
    }

    /// <summary>
    /// Writes zeros for the specified number of bits.
    /// </summary>
    /// <param name="bitCount">Number of zero bits to write.</param>
    public void WriteZeros(int bitCount)
    {
        if (_bitPosition + bitCount > TotalBits)
            throw new InvalidOperationException("Not enough bits remaining.");

        // Since we cleared the buffer initially, we just need to advance
        _bitPosition += bitCount;
    }

    /// <summary>
    /// Aligns the position to the next byte boundary with zero padding.
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
    public void Reset()
    {
        _bitPosition = 0;
        _data.Clear();
    }

    /// <summary>
    /// Gets the written data as a span.
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan => _data[..BytesWritten];
}

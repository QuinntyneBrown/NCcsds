using System.Buffers.Binary;

namespace NCcsds.Core.Buffers;

/// <summary>
/// Provides byte-level write operations on a span with big-endian support.
/// </summary>
public ref struct SpanWriter
{
    private readonly Span<byte> _data;
    private int _position;

    /// <summary>
    /// Creates a new span writer over the specified data.
    /// </summary>
    /// <param name="data">The data to write to.</param>
    public SpanWriter(Span<byte> data)
    {
        _data = data;
        _position = 0;
    }

    /// <summary>
    /// Gets the current position.
    /// </summary>
    public int Position => _position;

    /// <summary>
    /// Gets the capacity of the underlying buffer.
    /// </summary>
    public int Capacity => _data.Length;

    /// <summary>
    /// Gets the number of bytes remaining.
    /// </summary>
    public int Remaining => _data.Length - _position;

    /// <summary>
    /// Gets the number of bytes written.
    /// </summary>
    public int BytesWritten => _position;

    /// <summary>
    /// Writes a byte.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    public void WriteByte(byte value)
    {
        if (_position >= _data.Length)
            throw new InvalidOperationException("No more space to write.");
        _data[_position++] = value;
    }

    /// <summary>
    /// Writes an unsigned 16-bit integer (big-endian).
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt16BigEndian(ushort value)
    {
        if (_position + 2 > _data.Length)
            throw new InvalidOperationException("Not enough space to write.");
        BinaryPrimitives.WriteUInt16BigEndian(_data.Slice(_position, 2), value);
        _position += 2;
    }

    /// <summary>
    /// Writes an unsigned 32-bit integer (big-endian).
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt32BigEndian(uint value)
    {
        if (_position + 4 > _data.Length)
            throw new InvalidOperationException("Not enough space to write.");
        BinaryPrimitives.WriteUInt32BigEndian(_data.Slice(_position, 4), value);
        _position += 4;
    }

    /// <summary>
    /// Writes an unsigned 64-bit integer (big-endian).
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt64BigEndian(ulong value)
    {
        if (_position + 8 > _data.Length)
            throw new InvalidOperationException("Not enough space to write.");
        BinaryPrimitives.WriteUInt64BigEndian(_data.Slice(_position, 8), value);
        _position += 8;
    }

    /// <summary>
    /// Writes a signed 16-bit integer (big-endian).
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt16BigEndian(short value)
    {
        if (_position + 2 > _data.Length)
            throw new InvalidOperationException("Not enough space to write.");
        BinaryPrimitives.WriteInt16BigEndian(_data.Slice(_position, 2), value);
        _position += 2;
    }

    /// <summary>
    /// Writes a signed 32-bit integer (big-endian).
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt32BigEndian(int value)
    {
        if (_position + 4 > _data.Length)
            throw new InvalidOperationException("Not enough space to write.");
        BinaryPrimitives.WriteInt32BigEndian(_data.Slice(_position, 4), value);
        _position += 4;
    }

    /// <summary>
    /// Writes the specified bytes.
    /// </summary>
    /// <param name="bytes">The bytes to write.</param>
    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (_position + bytes.Length > _data.Length)
            throw new InvalidOperationException("Not enough space to write.");
        bytes.CopyTo(_data.Slice(_position, bytes.Length));
        _position += bytes.Length;
    }

    /// <summary>
    /// Writes zeros for the specified count.
    /// </summary>
    /// <param name="count">Number of zero bytes to write.</param>
    public void WriteZeros(int count)
    {
        if (_position + count > _data.Length)
            throw new InvalidOperationException("Not enough space to write.");
        _data.Slice(_position, count).Clear();
        _position += count;
    }

    /// <summary>
    /// Skips the specified number of bytes without writing.
    /// </summary>
    /// <param name="count">Number of bytes to skip.</param>
    public void Skip(int count)
    {
        if (_position + count > _data.Length)
            throw new InvalidOperationException("Not enough space to skip.");
        _position += count;
    }

    /// <summary>
    /// Resets the position to the beginning.
    /// </summary>
    public void Reset() => _position = 0;

    /// <summary>
    /// Seeks to the specified position.
    /// </summary>
    /// <param name="position">The position to seek to.</param>
    public void Seek(int position)
    {
        if (position < 0 || position > _data.Length)
            throw new ArgumentOutOfRangeException(nameof(position));
        _position = position;
    }

    /// <summary>
    /// Gets the written data as a span.
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan => _data[.._position];

    /// <summary>
    /// Gets the remaining writable span.
    /// </summary>
    public Span<byte> RemainingSpan => _data[_position..];
}

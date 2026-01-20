using System.Buffers.Binary;

namespace NCcsds.Core.Buffers;

/// <summary>
/// Provides byte-level read operations on a span with big-endian support.
/// </summary>
public ref struct SpanReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    /// <summary>
    /// Creates a new span reader over the specified data.
    /// </summary>
    /// <param name="data">The data to read from.</param>
    public SpanReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    /// <summary>
    /// Gets the current position.
    /// </summary>
    public int Position => _position;

    /// <summary>
    /// Gets the length of the underlying data.
    /// </summary>
    public int Length => _data.Length;

    /// <summary>
    /// Gets the number of bytes remaining.
    /// </summary>
    public int Remaining => _data.Length - _position;

    /// <summary>
    /// Gets whether the reader has reached the end.
    /// </summary>
    public bool IsAtEnd => _position >= _data.Length;

    /// <summary>
    /// Reads a byte.
    /// </summary>
    public byte ReadByte()
    {
        if (_position >= _data.Length)
            throw new InvalidOperationException("No more data to read.");
        return _data[_position++];
    }

    /// <summary>
    /// Reads an unsigned 16-bit integer (big-endian).
    /// </summary>
    public ushort ReadUInt16BigEndian()
    {
        if (_position + 2 > _data.Length)
            throw new InvalidOperationException("Not enough data to read.");
        var value = BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position, 2));
        _position += 2;
        return value;
    }

    /// <summary>
    /// Reads an unsigned 32-bit integer (big-endian).
    /// </summary>
    public uint ReadUInt32BigEndian()
    {
        if (_position + 4 > _data.Length)
            throw new InvalidOperationException("Not enough data to read.");
        var value = BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(_position, 4));
        _position += 4;
        return value;
    }

    /// <summary>
    /// Reads an unsigned 64-bit integer (big-endian).
    /// </summary>
    public ulong ReadUInt64BigEndian()
    {
        if (_position + 8 > _data.Length)
            throw new InvalidOperationException("Not enough data to read.");
        var value = BinaryPrimitives.ReadUInt64BigEndian(_data.Slice(_position, 8));
        _position += 8;
        return value;
    }

    /// <summary>
    /// Reads a signed 16-bit integer (big-endian).
    /// </summary>
    public short ReadInt16BigEndian()
    {
        if (_position + 2 > _data.Length)
            throw new InvalidOperationException("Not enough data to read.");
        var value = BinaryPrimitives.ReadInt16BigEndian(_data.Slice(_position, 2));
        _position += 2;
        return value;
    }

    /// <summary>
    /// Reads a signed 32-bit integer (big-endian).
    /// </summary>
    public int ReadInt32BigEndian()
    {
        if (_position + 4 > _data.Length)
            throw new InvalidOperationException("Not enough data to read.");
        var value = BinaryPrimitives.ReadInt32BigEndian(_data.Slice(_position, 4));
        _position += 4;
        return value;
    }

    /// <summary>
    /// Reads the specified number of bytes.
    /// </summary>
    /// <param name="count">Number of bytes to read.</param>
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        if (_position + count > _data.Length)
            throw new InvalidOperationException("Not enough data to read.");
        var span = _data.Slice(_position, count);
        _position += count;
        return span;
    }

    /// <summary>
    /// Reads bytes into the provided buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    public void ReadBytes(Span<byte> buffer)
    {
        if (_position + buffer.Length > _data.Length)
            throw new InvalidOperationException("Not enough data to read.");
        _data.Slice(_position, buffer.Length).CopyTo(buffer);
        _position += buffer.Length;
    }

    /// <summary>
    /// Peeks at the next byte without advancing position.
    /// </summary>
    public byte Peek()
    {
        if (_position >= _data.Length)
            throw new InvalidOperationException("No more data to peek.");
        return _data[_position];
    }

    /// <summary>
    /// Skips the specified number of bytes.
    /// </summary>
    /// <param name="count">Number of bytes to skip.</param>
    public void Skip(int count)
    {
        if (_position + count > _data.Length)
            throw new InvalidOperationException("Not enough data to skip.");
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
    /// Gets the remaining data as a span.
    /// </summary>
    public ReadOnlySpan<byte> RemainingSpan => _data[_position..];

    /// <summary>
    /// Gets the underlying data span.
    /// </summary>
    public ReadOnlySpan<byte> Data => _data;
}

namespace NCcsds.Core.Interfaces;

/// <summary>
/// Interface for encoding a value to binary data.
/// </summary>
/// <typeparam name="T">The type to encode.</typeparam>
public interface IEncoder<T>
{
    /// <summary>
    /// Gets the number of bytes required to encode the value.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The number of bytes required.</returns>
    int GetEncodedSize(T value);

    /// <summary>
    /// Encodes the value into the provided buffer.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="buffer">The buffer to write to.</param>
    /// <returns>The number of bytes written.</returns>
    int Encode(T value, Span<byte> buffer);

    /// <summary>
    /// Encodes the value to a new byte array.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded bytes.</returns>
    byte[] Encode(T value);
}

/// <summary>
/// Interface for types that can encode themselves.
/// </summary>
public interface IEncodable
{
    /// <summary>
    /// Gets the number of bytes required to encode this instance.
    /// </summary>
    int EncodedSize { get; }

    /// <summary>
    /// Encodes this instance into the provided buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <returns>The number of bytes written.</returns>
    int Encode(Span<byte> buffer);

    /// <summary>
    /// Encodes this instance to a new byte array.
    /// </summary>
    /// <returns>The encoded bytes.</returns>
    byte[] ToBytes();
}

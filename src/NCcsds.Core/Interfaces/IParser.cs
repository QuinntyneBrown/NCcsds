namespace NCcsds.Core.Interfaces;

/// <summary>
/// Interface for parsing binary data into a structured type.
/// </summary>
/// <typeparam name="T">The type to parse into.</typeparam>
public interface IParser<T>
{
    /// <summary>
    /// Parses the data and returns a result.
    /// </summary>
    /// <param name="data">The data to parse.</param>
    /// <returns>A result containing the parsed value or an error.</returns>
    Result<T> Parse(ReadOnlySpan<byte> data);

    /// <summary>
    /// Tries to parse the data.
    /// </summary>
    /// <param name="data">The data to parse.</param>
    /// <param name="value">The parsed value if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    bool TryParse(ReadOnlySpan<byte> data, out T? value);
}

/// <summary>
/// Interface for parsing binary data with position tracking.
/// </summary>
/// <typeparam name="T">The type to parse into.</typeparam>
public interface ISpanParser<T>
{
    /// <summary>
    /// Parses data starting at the specified offset.
    /// </summary>
    /// <param name="data">The data to parse.</param>
    /// <param name="offset">The offset to start parsing at.</param>
    /// <param name="bytesConsumed">The number of bytes consumed.</param>
    /// <returns>A result containing the parsed value or an error.</returns>
    Result<T> Parse(ReadOnlySpan<byte> data, int offset, out int bytesConsumed);
}

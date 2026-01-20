namespace NCcsds.Core.Exceptions;

/// <summary>
/// Base exception for all CCSDS-related errors.
/// </summary>
public class CcsdsException : Exception
{
    /// <summary>
    /// Creates a new CCSDS exception.
    /// </summary>
    public CcsdsException()
    {
    }

    /// <summary>
    /// Creates a new CCSDS exception with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CcsdsException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new CCSDS exception with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CcsdsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when frame parsing fails.
/// </summary>
public class FrameParseException : CcsdsException
{
    /// <summary>
    /// The position in the data where parsing failed.
    /// </summary>
    public int? Position { get; }

    /// <summary>
    /// Creates a new frame parse exception.
    /// </summary>
    public FrameParseException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new frame parse exception with position information.
    /// </summary>
    public FrameParseException(string message, int position) : base(message)
    {
        Position = position;
    }

    /// <summary>
    /// Creates a new frame parse exception with inner exception.
    /// </summary>
    public FrameParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when frame validation fails (e.g., CRC error).
/// </summary>
public class FrameValidationException : CcsdsException
{
    /// <summary>
    /// The type of validation that failed.
    /// </summary>
    public string? ValidationType { get; }

    /// <summary>
    /// Creates a new frame validation exception.
    /// </summary>
    public FrameValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new frame validation exception with validation type.
    /// </summary>
    public FrameValidationException(string message, string validationType) : base(message)
    {
        ValidationType = validationType;
    }
}

/// <summary>
/// Exception thrown when encoding fails.
/// </summary>
public class EncodingException : CcsdsException
{
    /// <summary>
    /// Creates a new encoding exception.
    /// </summary>
    public EncodingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new encoding exception with inner exception.
    /// </summary>
    public EncodingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when decoding fails.
/// </summary>
public class DecodingException : CcsdsException
{
    /// <summary>
    /// The position in the data where decoding failed.
    /// </summary>
    public int? Position { get; }

    /// <summary>
    /// Creates a new decoding exception.
    /// </summary>
    public DecodingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new decoding exception with position.
    /// </summary>
    public DecodingException(string message, int position) : base(message)
    {
        Position = position;
    }

    /// <summary>
    /// Creates a new decoding exception with inner exception.
    /// </summary>
    public DecodingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when configuration is invalid.
/// </summary>
public class ConfigurationException : CcsdsException
{
    /// <summary>
    /// The name of the configuration property that is invalid.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Creates a new configuration exception.
    /// </summary>
    public ConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new configuration exception with property name.
    /// </summary>
    public ConfigurationException(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
    }
}

/// <summary>
/// Exception thrown when a protocol error occurs.
/// </summary>
public class ProtocolException : CcsdsException
{
    /// <summary>
    /// The protocol that encountered the error.
    /// </summary>
    public string? Protocol { get; }

    /// <summary>
    /// Creates a new protocol exception.
    /// </summary>
    public ProtocolException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new protocol exception with protocol name.
    /// </summary>
    public ProtocolException(string message, string protocol) : base(message)
    {
        Protocol = protocol;
    }

    /// <summary>
    /// Creates a new protocol exception with inner exception.
    /// </summary>
    public ProtocolException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

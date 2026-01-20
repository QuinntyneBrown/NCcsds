namespace NCcsds.Core.Identifiers;

/// <summary>
/// Represents the Transfer Frame Version Number (TFVN).
/// </summary>
public readonly record struct TransferFrameVersionNumber : IComparable<TransferFrameVersionNumber>
{
    /// <summary>
    /// TM Transfer Frame version (00).
    /// </summary>
    public static readonly TransferFrameVersionNumber TM = new(0);

    /// <summary>
    /// AOS Transfer Frame version (01).
    /// </summary>
    public static readonly TransferFrameVersionNumber AOS = new(1);

    /// <summary>
    /// TC Transfer Frame version (00).
    /// </summary>
    public static readonly TransferFrameVersionNumber TC = new(0);

    /// <summary>
    /// Maximum valid TFVN value (2 bits).
    /// </summary>
    public const byte MaxValue = 0x03;

    /// <summary>
    /// The raw TFVN value.
    /// </summary>
    public byte Value { get; }

    /// <summary>
    /// Creates a new transfer frame version number.
    /// </summary>
    /// <param name="value">The TFVN value (0-3).</param>
    public TransferFrameVersionNumber(byte value)
    {
        if (value > MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), value, $"TFVN must be between 0 and {MaxValue}.");
        Value = value;
    }

    public int CompareTo(TransferFrameVersionNumber other) => Value.CompareTo(other.Value);

    public override string ToString() => $"TFVN:{Value}";

    public static implicit operator byte(TransferFrameVersionNumber tfvn) => tfvn.Value;
    public static explicit operator TransferFrameVersionNumber(byte value) => new(value);
}

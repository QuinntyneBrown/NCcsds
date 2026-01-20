namespace NCcsds.Core.Identifiers;

/// <summary>
/// Represents a CCSDS Virtual Channel Identifier (VCID).
/// Valid range is 0-63 for TM (6 bits) or 0-7 for TC (3 bits).
/// </summary>
public readonly record struct VirtualChannelId : IComparable<VirtualChannelId>
{
    /// <summary>
    /// Maximum valid VCID for TM frames (6 bits).
    /// </summary>
    public const byte MaxValueTm = 0x3F;

    /// <summary>
    /// Maximum valid VCID for TC frames (3 bits).
    /// </summary>
    public const byte MaxValueTc = 0x07;

    /// <summary>
    /// The raw virtual channel ID value.
    /// </summary>
    public byte Value { get; }

    /// <summary>
    /// Creates a new virtual channel ID.
    /// </summary>
    /// <param name="value">The virtual channel ID value.</param>
    public VirtualChannelId(byte value)
    {
        if (value > MaxValueTm)
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Virtual Channel ID must be between 0 and {MaxValueTm}.");
        Value = value;
    }

    /// <summary>
    /// Validates that this VCID is valid for TC frames.
    /// </summary>
    public bool IsValidForTc => Value <= MaxValueTc;

    /// <summary>
    /// Tries to create a virtual channel ID from the given value.
    /// </summary>
    public static bool TryCreate(byte value, out VirtualChannelId vcid)
    {
        if (value > MaxValueTm)
        {
            vcid = default;
            return false;
        }
        vcid = new VirtualChannelId(value);
        return true;
    }

    public int CompareTo(VirtualChannelId other) => Value.CompareTo(other.Value);

    public override string ToString() => $"VCID:{Value}";

    public static implicit operator byte(VirtualChannelId vcid) => vcid.Value;
    public static explicit operator VirtualChannelId(byte value) => new(value);
}

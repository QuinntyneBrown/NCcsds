namespace NCcsds.Core.Identifiers;

/// <summary>
/// Represents a CCSDS Spacecraft Identifier (SCID).
/// Valid range is 0-1023 (10 bits).
/// </summary>
public readonly record struct SpacecraftId : IComparable<SpacecraftId>
{
    /// <summary>
    /// Maximum valid spacecraft ID value (10 bits).
    /// </summary>
    public const ushort MaxValue = 0x3FF;

    /// <summary>
    /// The raw spacecraft ID value.
    /// </summary>
    public ushort Value { get; }

    /// <summary>
    /// Creates a new spacecraft ID.
    /// </summary>
    /// <param name="value">The spacecraft ID value (0-1023).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value exceeds 1023.</exception>
    public SpacecraftId(ushort value)
    {
        if (value > MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Spacecraft ID must be between 0 and {MaxValue}.");
        Value = value;
    }

    /// <summary>
    /// Tries to create a spacecraft ID from the given value.
    /// </summary>
    public static bool TryCreate(ushort value, out SpacecraftId scid)
    {
        if (value > MaxValue)
        {
            scid = default;
            return false;
        }
        scid = new SpacecraftId(value);
        return true;
    }

    public int CompareTo(SpacecraftId other) => Value.CompareTo(other.Value);

    public override string ToString() => $"SCID:{Value}";

    public static implicit operator ushort(SpacecraftId scid) => scid.Value;
    public static explicit operator SpacecraftId(ushort value) => new(value);
}
